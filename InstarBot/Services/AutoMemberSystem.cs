using System.Collections.Concurrent;
using System.Runtime.Caching;
using System.Timers;
using Discord;
using Discord.WebSocket;
using PaxAndromeda.Instar.Caching;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Metrics;
using PaxAndromeda.Instar.Modals;
using Serilog;
using Timer = System.Timers.Timer;

namespace PaxAndromeda.Instar.Services;

public sealed class AutoMemberSystem : IAutoMemberSystem
{
    private readonly MemoryCache _ddbCache = new("AutoMemberSystem_DDBCache");
    private readonly MemoryCache<MessageProperties> _messageCache = new("AutoMemberSystem_MessageCache");
    private readonly ConcurrentDictionary<ulong, bool> _introductionPosters = new();
    private readonly ConcurrentDictionary<ulong, bool> _punishedUsers = new();
    
    private DateTime _earliestJoinTime;

    private readonly IDynamicConfigService _dynamicConfig;
    private readonly IDiscordService _discord;
    private readonly IGaiusAPIService _gaiusApiService;
    private readonly IInstarDDBService _ddbService;
    private readonly IMetricService _metricService;
    private Timer _timer = null!;

    /// <summary>
    /// Recent messages per the last AMS run
    /// </summary>
    private Dictionary<ulong, int>? _recentMessages;

    public AutoMemberSystem(IDynamicConfigService dynamicConfig, IDiscordService discord, IGaiusAPIService gaiusApiService,
        IInstarDDBService ddbService, IMetricService metricService)
    {
        _dynamicConfig = dynamicConfig;
        _discord = discord;
        _gaiusApiService = gaiusApiService;
        _ddbService = ddbService;
        _metricService = metricService;

        discord.UserJoined += HandleUserJoined;
		discord.UserUpdated += HandleUserUpdated;
        discord.MessageReceived += HandleMessageReceived;
        discord.MessageDeleted += HandleMessageDeleted;

        Task.Run(Initialize).Wait();
    }

    private async Task Initialize()
    {
        var cfg = await _dynamicConfig.GetConfig();
        
        _earliestJoinTime = DateTime.UtcNow - TimeSpan.FromSeconds(cfg.AutoMemberConfig.MinimumJoinAge);
        
        await PreloadMessageCache(cfg);
        await PreloadIntroductionPosters(cfg);

        if (cfg.AutoMemberConfig.EnableGaiusCheck)
            await PreloadGaiusPunishments();

        StartTimer();
    }

    private async Task UpdateGaiusPunishments()
    {
        // Normally we'd go for 1 hour here, but we can run into
        // a situation where someone was warned exactly 1.000000001
        // hours ago, thus would be missed.  To fix this, we'll
        // bias for an hour and a half ago.
        var afterTime = DateTime.UtcNow - TimeSpan.FromHours(1.5);
        
        foreach (var warning in await _gaiusApiService.GetWarningsAfter(afterTime))
            _punishedUsers.TryAdd(warning.UserID.ID, true);
        foreach (var caselog in await _gaiusApiService.GetCaselogsAfter(afterTime))
            _punishedUsers.TryAdd(caselog.UserID.ID, true);
    }

    private async Task HandleMessageDeleted(Snowflake arg)
    {
        await _metricService.Emit(Metric.Discord_MessagesDeleted, 1);
        
        if (!_messageCache.Contains(arg.ID.ToString()))
            return;

        _messageCache.Remove(arg.ID.ToString());
        await _metricService.Emit(Metric.AMS_CachedMessages, _messageCache.GetCount());
    }

    private async Task HandleMessageReceived(IMessage arg)
    {
        var cfg = await _dynamicConfig.GetConfig();
        
        ulong guildId = 0;
        if (arg.Author is SocketGuildUser guildUser)
            guildId = guildUser.Guild.Id;
        
        var mp = new MessageProperties(arg.Author.Id, arg.Channel.Id, guildId);
        _messageCache.Add(arg.Id.ToString(), mp, new CacheItemPolicy
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(cfg.AutoMemberConfig.MinimumMessageTime)
        });

        await _metricService.Emit(Metric.Discord_MessagesSent, 1);
        await _metricService.Emit(Metric.AMS_CachedMessages, _messageCache.GetCount());

        if (!arg.Channel.Id.Equals(cfg.AutoMemberConfig.IntroductionChannel.ID)) 
            return;
        
        // Ignore members
        if (arg.Author is IGuildUser sgUser && sgUser.RoleIds.Contains(cfg.MemberRoleID.ID))
            return;
            
        _introductionPosters.TryAdd(arg.Author.Id, true);
    }

    private async Task HandleUserJoined(IGuildUser user)
    {
        var cfg = await _dynamicConfig.GetConfig();
        
        var dbUser = await _ddbService.GetUserAsync(user.Id);
        if (dbUser is null)
        {
            // Let's create a new user
            await _ddbService.CreateUserAsync(InstarUserData.CreateFrom(user));
        }
        else
        {
            switch (dbUser.Data.Position)
            {
                case InstarUserPosition.NewMember:
                case InstarUserPosition.Unknown:
                    await user.AddRoleAsync(cfg.NewMemberRoleID);
                    dbUser.Data.Position = InstarUserPosition.NewMember;
                    await dbUser.UpdateAsync();
                    break;
                
                default:
                    // Yes, they were a member
                    Log.Information("User {UserID} has been granted membership before.  Granting membership again", user.Id);
                    await GrantMembership(cfg, user, dbUser);
                    break;
            }
        }
        
        await _metricService.Emit(Metric.Discord_UsersJoined, 1);
    }
	
	private async Task HandleUserUpdated(UserUpdatedEventArgs arg)
	{
		if (!arg.HasUpdated)
			return;

		var user = await _ddbService.GetUserAsync(arg.ID);
		if (user is null)
		{
			// new user for the database, create from the latest data and return
			try
			{
				await _ddbService.CreateUserAsync(InstarUserData.CreateFrom(arg.After));
				Log.Information("Created new user {Username} (user ID {UserID})", arg.After.Username, arg.ID);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to create user with ID {UserID}, username {Username}", arg.ID, arg.After.Username);
			}

			return;
		}

		// Update the record
		bool changed = false;

		if (arg.Before.Username != arg.After.Username)
		{
			user.Data.Username = arg.After.Username;
			changed = true;
		}

		if (arg.Before.Nickname != arg.After.Nickname)
		{
			user.Data.Nicknames?.Add(new InstarUserDataHistoricalEntry<string>(DateTime.UtcNow, arg.After.Nickname));
			changed = true;
		}

		if (changed)
		{
			Log.Information("Updated metadata for user {Username} (user ID {UserID})", arg.After.Username, arg.ID);
			await user.UpdateAsync();
		}
	}

	private void StartTimer()
    {
        // Since we can start the bot in the middle of an hour,
        // first we must determine the time until the next top
        // of hour.
        var nextHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
            DateTime.UtcNow.Hour, 0, 0).AddHours(1);
        var millisecondsRemaining = (nextHour - DateTime.UtcNow).TotalMilliseconds;
        
        // Start the timer.  In elapsed step, we reset the
        // duration to exactly 1 hour.
        _timer = new Timer(millisecondsRemaining);
        _timer.Elapsed += TimerElapsed;
        _timer.Start();
    }

    private async void TimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            // Ensure the timer's interval is exactly 1 hour
            _timer.Interval = 60 * 60 * 1000;

            await RunAsync();
        }
        catch
        {
            // ignore
        }
    }
    
    public async Task RunAsync()
    {
        try
        {
            await _metricService.Emit(Metric.AMS_Runs, 1);
            var cfg = await _dynamicConfig.GetConfig();
            
            // Caution: This is an extremely long-running method!
            Log.Information("Beginning auto member routine");

            if (cfg.AutoMemberConfig.EnableGaiusCheck)
            {
                Log.Information("Updating Gaius database");
                await UpdateGaiusPunishments();
            }

            _earliestJoinTime = DateTime.UtcNow - TimeSpan.FromSeconds(cfg.AutoMemberConfig.MinimumJoinAge);
            _recentMessages = GetMessagesSent();
            
            Log.Verbose("Earliest join time: {EarliestJoinTime}", _earliestJoinTime);

            var users = await _discord.GetAllUsers();
            
            // Filter for new members that joined more than 1 day ago and have the correct roles
            var newMembers = users
                .Where(user => user.RoleIds.Contains(cfg.NewMemberRoleID.ID)).ToList();
            
            await _metricService.Emit(Metric.AMS_NewMembers, newMembers.Count);
            Log.Verbose("There are {NumNewMembers} users with the New Member role", newMembers.Count);

            var membershipGrants = 0;

			var eligibleMembers = newMembers.Where(user => CheckEligibility(cfg, user) == MembershipEligibility.Eligible)
				.ToDictionary(n => new Snowflake(n.Id), n => n);
            
            Log.Verbose("There are {NumNewMembers} users eligible for membership", eligibleMembers.Count);

            // Batch get users to save bandwidth
            var userData = (
				await _ddbService.GetBatchUsersAsync(
					eligibleMembers.Select(n => n.Key))
				)
                .Select(x => (x.Data.UserID!, x))
                .ToDictionary();

			// Determine which users are not present in DDB and need to be created
			var usersToCreate = eligibleMembers.Where(n => !userData.ContainsKey(n.Key)).Select(n => n.Value);

			// Step 1: Create missing users in DDB
			await foreach (var (id, user) in CreateMissingUsers(usersToCreate))
				userData.Add(id, user);

			// Step 2: Grant membership to eligible users
			foreach (var (id, dbUser) in userData)
			{
				try
				{
					// User has all the qualifications, let's update their role
					if (!eligibleMembers.TryGetValue(id, out var user))
						throw new BadStateException("Unexpected state: expected ID is missing from eligibleMembers");

					await GrantMembership(cfg, user, dbUser);
					membershipGrants++;

					Log.Information("Granted {UserId} membership", user.Id);
				} catch (Exception ex)
				{
					Log.Warning(ex, "Failed to grant user {UserId} membership", id);
				}
			}
            
            await _metricService.Emit(Metric.AMS_UsersGrantedMembership, membershipGrants);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Auto member system failed to run");
        }
    }

	private async IAsyncEnumerable<KeyValuePair<Snowflake, InstarDatabaseEntry<InstarUserData>>> CreateMissingUsers(IEnumerable<IGuildUser> users)
	{
		foreach (var user in users)
		{
			InstarDatabaseEntry<InstarUserData>? dbUser;
			try
			{
				await _ddbService.CreateUserAsync(InstarUserData.CreateFrom(user));

				// Now, get the user we just created
				dbUser = await _ddbService.GetUserAsync(user.Id);

				if (dbUser is null)
				{
					// Welp, something's wrong with DynamoDB that isn't throwing an
					// exception with CreateUserAsync or GetUserAsync. At this point,
					// we expect the user to be present in DynamoDB, so we'll treat
					// this as an error.
					throw new BadStateException("Expected user to be created and returned from DynamoDB");
				}
			} catch (Exception ex)
			{
				await _metricService.Emit(Metric.AMS_DynamoFailures, 1);
				Log.Error(ex, "Failed to get or create user with ID {UserID} in DynamoDB", user.Id);
				continue;
			}

			yield return new KeyValuePair<Snowflake, InstarDatabaseEntry<InstarUserData>>(user.Id, dbUser);
		}
	}

	private async Task GrantMembership(InstarDynamicConfiguration cfg, IGuildUser user,
        InstarDatabaseEntry<InstarUserData> dbUser)
    {
        await user.AddRoleAsync(cfg.MemberRoleID);
        await user.RemoveRoleAsync(cfg.NewMemberRoleID);
        
        dbUser.Data.Position = InstarUserPosition.Member;
        await dbUser.UpdateAsync();

        // Remove the cache entry
        if (_ddbCache.Contains(user.Id.ToString()))
            _ddbCache.Remove(user.Id.ToString());
        
        // Remove introduction reference, if it exists
        _introductionPosters.TryRemove(user.Id, out _);
    }

	/// <summary>
	/// Determines the eligibility of a user for membership based on specific criteria.
	/// </summary>
	/// <param name="cfg">The current configuration from AppConfig.</param>
	/// <param name="user">The user whose eligibility is being evaluated.</param>
	/// <returns>An enumeration value of type <see cref="MembershipEligibility"/> that indicates the user's membership eligibility status.</returns>
	/// <remarks>
	///     The criteria for membership is as follows:
	/// <list type="bullet">
	///     <item>The user must have the required roles (see <see cref="CheckUserRequiredRoles"/>)</item>
	///     <item>The user must be on the server for a configurable minimum amount of time</item>
	///     <item>The user must have posted an introduction</item>
	///     <item>The user must have posted enough messages in a configurable amount of time</item>
	///     <item>The user must not have been issued a moderator action</item>
	///     <item>The user must not already be a member</item>
	/// </list>
	/// </remarks>
	public MembershipEligibility CheckEligibility(InstarDynamicConfiguration cfg, IGuildUser user)
    {
        // We need recent messages here, so load it into
        // context if it does not exist, such as when the
        // bot first starts and has not run AMS yet.
        _recentMessages = GetMessagesSent();

        var eligibility = MembershipEligibility.Eligible;

        if (user.RoleIds.Contains(cfg.MemberRoleID.ID))
            eligibility |= MembershipEligibility.AlreadyMember;

        if (user.JoinedAt > _earliestJoinTime)
            eligibility |= MembershipEligibility.TooYoung;

        if (!CheckUserRequiredRoles(cfg, user))
            eligibility |= MembershipEligibility.MissingRoles;

        if (!_introductionPosters.ContainsKey(user.Id))
            eligibility |=  MembershipEligibility.MissingIntroduction;

        if (_recentMessages.TryGetValue(user.Id, out var messages) && messages < cfg.AutoMemberConfig.MinimumMessages)
            eligibility |=  MembershipEligibility.NotEnoughMessages;

        if (_punishedUsers.ContainsKey(user.Id))
            eligibility |= MembershipEligibility.PunishmentReceived;

		if (user.RoleIds.Contains(cfg.AutoMemberConfig.HoldRole))
			eligibility |= MembershipEligibility.AutoMemberHold;

		if (eligibility != MembershipEligibility.Eligible)
		{
			eligibility &= ~MembershipEligibility.Eligible;
			eligibility |= MembershipEligibility.NotEligible;
		}
        
        Log.Verbose("User {User} ({UserID}) membership eligibility: {Eligibility}", user.Username, user.Id, eligibility);
        return eligibility;
    }

    /// <summary>
    /// Verifies if a user possesses the required roles for automatic membership based on the provided configuration.
    /// </summary>
    /// <param name="cfg">The dynamic configuration containing role requirements and settings for automatic membership.</param>
    /// <param name="user">The user whose roles are being checked against the configuration.</param>
    /// <returns>True if the user satisfies the role requirements; otherwise, false.</returns>
    private static bool CheckUserRequiredRoles(InstarDynamicConfiguration cfg, IGuildUser user)
    {
        // Auto Member Hold overrides all role permissions
        if (user.RoleIds.Contains(cfg.AutoMemberConfig.HoldRole.ID))
            return false;
        
        return cfg.AutoMemberConfig.RequiredRoles.All(
            roleGroup => roleGroup.Roles.Select(n => n.ID)
                .Intersect(user.RoleIds).Any()
            );
    }

    private Dictionary<ulong, int> GetMessagesSent()
    {
        var map = _messageCache
            .Cast<KeyValuePair<string, MessageProperties>>() // Cast to access LINQ extensions
            .Select(entry => entry.Value)
            .GroupBy(properties => properties.UserID)
            .ToDictionary(group => group.Key, group => group.Count());

        return map;
    }
    
    private async Task PreloadGaiusPunishments()
    {
        foreach (var warning in await _gaiusApiService.GetAllWarnings())
            _punishedUsers.TryAdd(warning.UserID.ID, true);
        foreach (var caselog in await _gaiusApiService.GetAllCaselogs())
            _punishedUsers.TryAdd(caselog.UserID.ID, true);
    }

    private async Task PreloadMessageCache(InstarDynamicConfiguration cfg)
    {
        Log.Information("Preloading message cache...");
        var guild = _discord.GetGuild();
        var earliestMessageTime = DateTime.UtcNow - TimeSpan.FromSeconds(cfg.AutoMemberConfig.MinimumMessageTime);
        var messages = _discord.GetMessages(guild, earliestMessageTime);

        await foreach (var message in messages)
        {
            var mp = new MessageProperties(message.Author.Id, message.Channel?.Id ?? 0, guild.Id);
            _messageCache.Add(message.Id.ToString(), mp, new CacheItemPolicy
            {
                AbsoluteExpiration = message.Timestamp + TimeSpan.FromSeconds(cfg.AutoMemberConfig.MinimumMessageTime)
            });
        }
        Log.Information("Done preloading message cache!");
    }

    private async Task PreloadIntroductionPosters(InstarDynamicConfiguration cfg)
    {
        if (await _discord.GetChannel(cfg.AutoMemberConfig.IntroductionChannel) is not ITextChannel introChannel)
            throw new InvalidOperationException("Introductions channel not found");

        var messages = (await introChannel.GetMessagesAsync().FlattenAsync()).GetEnumerator();

        // Assumption:  Last message is the oldest one
        while (messages.MoveNext()) // Move to the first message, if there is any
        {
            IMessage? message;
            IMessage? oldestMessage = null;

            do
            {
                message = messages.Current;
                if (message is null)
                    break;

                if (message.Author is IGuildUser sgUser && sgUser.RoleIds.Contains(cfg.MemberRoleID.ID))
                    continue;

                _introductionPosters.TryAdd(message.Author.Id, true);

                if (oldestMessage is null || message.Timestamp < oldestMessage.Timestamp)
                    oldestMessage = message;
            } while (messages.MoveNext());

            if (message is null || oldestMessage is null)
                break;

            messages = (await introChannel.GetMessagesAsync(oldestMessage, Direction.Before).FlattenAsync()).GetEnumerator();
        }

        messages.Dispose();
    }
}