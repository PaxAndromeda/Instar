using System.Collections.ObjectModel;
using Discord;
using InstarBot.Test.Framework.Models;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.Services;

namespace InstarBot.Test.Framework;

public class TestDiscordContext
{
	public Snowflake GuildId { get; }
	public ReadOnlyCollection<TestGuildUser> Users { get; }
	public ReadOnlyCollection<TestChannel> Channels { get; }
	public ReadOnlyCollection<TestRole> Roles { get; }

	public TestDiscordContext(Snowflake guildId, IEnumerable<TestGuildUser> users, IEnumerable<TestChannel> channels, IEnumerable<TestRole> roles)
	{
		GuildId = guildId;
		Users = users.ToList().AsReadOnly();
		Channels = channels.ToList().AsReadOnly();
		Roles = roles.ToList().AsReadOnly();
	}

	public static TestDiscordContextBuilder Builder => new();
}

public class TestDiscordContextBuilder : IBuilder<TestDiscordContext>
{
	private Snowflake _guildId = Snowflake.Generate();
	private readonly Dictionary<Snowflake, TestGuildUser> _registeredUsers = new();
	private readonly Dictionary<Snowflake, TestChannel> _registeredChannels = new();
	private readonly Dictionary<Snowflake, TestRole> _registeredRoles = new();

	public TestDiscordContext Build()
	{
		return new TestDiscordContext(_guildId, _registeredUsers.Values, _registeredChannels.Values, _registeredRoles.Values);
	}

	public async Task<TestDiscordContextBuilder> LoadFromConfig(IDynamicConfigService configService)
	{
		var cfg = await configService.GetConfig();

		_guildId = cfg.TargetGuild;
		RegisterUser(cfg.BotUserID, cfg.BotName);

		RegisterChannel(cfg.TargetChannel);
		RegisterChannel(cfg.StaffAnnounceChannel);

		RegisterRole(cfg.StaffRoleID, "Staff");
		RegisterRole(cfg.NewMemberRoleID, "New Member");
		RegisterRole(cfg.MemberRoleID, "Member");
		
		foreach (Snowflake snowflake in cfg.AuthorizedStaffID)
			RegisterRole(snowflake);

		LoadFromAutoMemberConfig(cfg.AutoMemberConfig);
		LoadFromBirthdayConfig(cfg.BirthdayConfig);
		LoadFromTeamsConfig(cfg.Teams);

		return this;
	}

	private void LoadFromAutoMemberConfig(AutoMemberConfig cfg)
	{
		RegisterRole(cfg.HoldRole, "AMH");
		RegisterChannel(cfg.IntroductionChannel);

		foreach (Snowflake roleId in cfg.RequiredRoles.SelectMany(n => n.Roles))
			RegisterRole(roleId); // no names here sadly
	}

	private void LoadFromBirthdayConfig(BirthdayConfig cfg)
	{
		RegisterRole(cfg.BirthdayRole, "Happy Birthday!");
		RegisterChannel(cfg.BirthdayAnnounceChannel);

		foreach (Snowflake snowflake in cfg.AgeRoleMap.Select(n => n.Role))
			RegisterRole(snowflake);
	}

	private void LoadFromTeamsConfig(IEnumerable<Team> teams)
	{
		foreach (Team team in teams)
		{
			RegisterRole(team.ID);
			RegisterUser(team.Teamleader);
		}
	}

	private void RegisterChannel(Snowflake snowflake)
	{
		if (_registeredChannels.ContainsKey(snowflake))
			return;

		_registeredChannels.Add(snowflake, new TestChannel(snowflake));
	}

	private void RegisterRole(Snowflake snowflake, string name = "Role")
	{
		if (_registeredRoles.ContainsKey(snowflake))
			return;

		_registeredRoles.Add(snowflake, new TestRole(snowflake)
		{
			Name = name
		});
	}

	private void RegisterUser(Snowflake snowflake, string name = "User")
	{
		if (_registeredUsers.ContainsKey(snowflake))
			return;

		_registeredUsers.Add(snowflake, new TestGuildUser(snowflake)
		{
			GlobalName = name,
			DisplayName = name,
			Username = name,
			Nickname = name
		});
	}

	public TestDiscordContextBuilder RegisterUser(TestGuildUser user)
	{
		_registeredUsers.Add(user.Id, user);
		return this;
	}

	internal bool TryGetUser(Snowflake userId, out TestGuildUser testGuildUser)
	{
		return _registeredUsers.TryGetValue(userId, out testGuildUser);
	}
}