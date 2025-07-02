using System.Collections.Concurrent;
using System.Runtime.Caching;
using System.Timers;
using Discord;
using Discord.WebSocket;
using PaxAndromeda.Instar.Caching;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.Metrics;
using Serilog;
using Timer = System.Timers.Timer;

namespace PaxAndromeda.Instar.Services;

public sealed class AutoMemberSystem
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
        
        if (await WasUserGrantedMembershipBefore(user.Id))
        {
            Log.Information("User {UserID} has been granted membership before.  Granting membership again", user.Id);
            await GrantMembership(cfg, user);
        }
        else
        {
            await user.AddRoleAsync(cfg.NewMemberRoleID);
        }
        
        await _metricService.Emit(Metric.Discord_UsersJoined, 1);
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
            
            // Caution:  This is an extremely long-running method!
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

            newMembers = await newMembers.ToAsyncEnumerable()
                .WhereAwait(async user => await CheckEligibility(user) == MembershipEligibility.Eligible).ToListAsync();
            
            Log.Verbose("There are {NumNewMembers} users eligible for membership", newMembers.Count);
                
            foreach (var user in newMembers)
            {
                // User has all the qualifications, let's update their role
                try
                {
                    await GrantMembership(cfg, user);
                    membershipGrants++;
                    
                    Log.Information("Granted {UserId} membership", user.Id);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to grant user {UserId} membership", user.Id);
                }
            }
            
            await _metricService.Emit(Metric.AMS_UsersGrantedMembership, membershipGrants);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Auto member system failed to run");
        }
    }

    private async Task<bool> WasUserGrantedMembershipBefore(Snowflake snowflake)
    {
        if (_ddbCache.Contains(snowflake.ID.ToString()) && (bool)_ddbCache[snowflake.ID.ToString()])
            return true;

        var grantedMembership = await _ddbService.GetUserMembership(snowflake.ID);
        if (grantedMembership is null)
            return false;
        
        // Cache for 6-hour sliding window.  If accessed, time is reset.
        _ddbCache.Add(snowflake.ID.ToString(), grantedMembership.Value, new CacheItemPolicy
        {
            SlidingExpiration = TimeSpan.FromHours(6)
        });

        return grantedMembership.Value;
    }

    private async Task GrantMembership(InstarDynamicConfiguration cfg, IGuildUser user)
    {
        await user.AddRoleAsync(cfg.MemberRoleID);
        await user.RemoveRoleAsync(cfg.NewMemberRoleID);
        await _ddbService.UpdateUserMembership(user.Id, true);

        // Remove the cache entry
        if (_ddbCache.Contains(user.Id.ToString()))
            _ddbCache.Remove(user.Id.ToString());
        
        // Remove introduction reference, if it exists
        _introductionPosters.TryRemove(user.Id, out _);
    }

    public async Task<MembershipEligibility> CheckEligibility(IGuildUser user)
    {
        var cfg = await _dynamicConfig.GetConfig();
        
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

        if (_recentMessages.TryGetValue(user.Id, out int messages) && messages < cfg.AutoMemberConfig.MinimumMessages)
            eligibility |=  MembershipEligibility.NotEnoughMessages;

        if (_punishedUsers.ContainsKey(user.Id))
            eligibility |= MembershipEligibility.PunishmentReceived;

        if (eligibility != MembershipEligibility.Eligible)
            eligibility |= MembershipEligibility.NotEligible;
        
        Log.Verbose("User {User} ({UserID}) membership eligibility: {Eligibility}", user.Username, user.Id, eligibility);
        return eligibility;
    }

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
        var map = this._messageCache
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