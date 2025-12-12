using Amazon.DynamoDBv2.DataModel;
using FluentAssertions;
using InstarBot.Tests.Models;
using InstarBot.Tests.Services;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Services;
using Xunit;
using Metric = PaxAndromeda.Instar.Metrics.Metric;

namespace InstarBot.Tests.Integration.Services;

public static class BirthdaySystemTests
{
	private static async Task<Context> Setup(DateTime todayLocal, DateTimeOffset? birthdate = null, bool applyBirthday = false, Func<List<ulong>, InstarDynamicConfiguration, List<ulong>>? roleUpdateFn = null)
	{
		var today = todayLocal.ToUniversalTime();
		var timeProviderMock = new Mock<TimeProvider>();
		timeProviderMock.Setup(n => n.GetUtcNow()).Returns(today);

		Birthday? birthday = birthdate is null ? null : new Birthday(birthdate.Value, timeProviderMock.Object);

		var testUserId = Snowflake.Generate();
		var cfg = TestUtilities.GetDynamicConfiguration();
		var mockDDB = new MockInstarDDBService();
		var metrics = new MockMetricService();
		var polledCfg = await cfg.GetConfig();

		var discord = TestUtilities.SetupDiscordService(new TestContext
		{
			UserID = Snowflake.Generate(),
			Channels =
			{
				{ polledCfg.BirthdayConfig.BirthdayAnnounceChannel, new TestChannel(polledCfg.BirthdayConfig.BirthdayAnnounceChannel) }
			}
		}) as MockDiscordService;

		discord.Should().NotBeNull();

		var user = new TestGuildUser
		{
			Id = testUserId,
			Username = "TestUser"
		};

		var rolesToAdd = new List<ulong>(); // Some random role

		if (applyBirthday)
			rolesToAdd.Add(polledCfg.BirthdayConfig.BirthdayRole);

		if (birthday is not null)
		{
			int priorYearsOld = birthday.Age - 1;
			var ageRole = polledCfg.BirthdayConfig.AgeRoleMap
				.OrderByDescending(n => n.Age)
				.SkipWhile(n => n.Age > priorYearsOld)
				.First();

			rolesToAdd.Add(ageRole.Role.ID);
		}

		if (roleUpdateFn is not null)
			rolesToAdd = roleUpdateFn(rolesToAdd, polledCfg);

		await user.AddRolesAsync(rolesToAdd);


		discord.AddUser(user);

		var dbUser = InstarUserData.CreateFrom(user);

		if (birthday is not null)
		{
			dbUser.Birthday = birthday;
			dbUser.Birthdate = birthday.Key;

			if (birthday.IsToday)
			{
				mockDDB.Setup(n => n.GetUsersByBirthday(today, It.IsAny<TimeSpan>()))
					.ReturnsAsync([new InstarDatabaseEntry<InstarUserData>(Mock.Of<IDynamoDBContext>(), dbUser)]);
			}
		}

		await mockDDB.CreateUserAsync(dbUser);



		var birthdaySystem = new BirthdaySystem(cfg, discord, mockDDB, metrics, timeProviderMock.Object);

		return new Context(testUserId, birthdaySystem, mockDDB, discord, metrics, polledCfg, birthday);
	}

	private static bool IsDateMatch(DateTime a, DateTime b)
	{
		// Match everything but year
		var aUtc = a.ToUniversalTime();
		var bUtc = b.ToUniversalTime();

		return aUtc.Month == bUtc.Month && aUtc.Day == bUtc.Day && aUtc.Hour == bUtc.Hour;
	}

	[Fact]
	public static async Task BirthdaySystem_WhenUserBirthday_ShouldGrantRole()
	{
		// Arrange
		var birthday = DateTime.Parse("2000-02-14T00:00:00Z");
		var today = DateTime.Parse("2025-02-14T00:00:00Z");

		var ctx = await Setup(today, birthday);

		// Act
		await ctx.System.RunAsync();

		// Assert
		// We expect a few things here: the test user should now have the birthday
		// role, and there should now be a message in the birthday announce channel.

		var user = ctx.Discord.GetUser(ctx.TestUserId);
		user.Should().NotBeNull();
		user.RoleIds.Should().Contain(ctx.Cfg.BirthdayConfig.BirthdayRole);
		user.RoleIds.Should().Contain(ctx.Cfg.BirthdayConfig.AgeRoleMap.MaxBy(n => n.Age)!.Role.ID);

		var channel = await ctx.Discord.GetChannel(ctx.Cfg.BirthdayConfig.BirthdayAnnounceChannel) as TestChannel;
		channel.Should().NotBeNull();

		var messages = await channel.GetMessagesAsync().SelectMany(n => n).ToListAsync();
		messages.Count.Should().BeGreaterThan(0);
		TestUtilities.MatchesFormat(Strings.Birthday_Announcement, messages[0].Content);

		ctx.Metrics.GetMetricValues(Metric.BirthdaySystem_Failures).Sum().Should().Be(0);
		ctx.Metrics.GetMetricValues(Metric.BirthdaySystem_Grants).Sum().Should().Be(1);
	}

	[Fact]
	public static async Task BirthdaySystem_WhenUserBirthday_ShouldUpdateAgeRoles()
	{
		// Arrange
		var birthdate = DateTime.Parse("2000-02-14T00:00:00Z");
		var today = DateTime.Parse("2016-02-14T00:00:00Z");

		var ctx = await Setup(today, birthdate);

		int yearsOld = ctx.Birthday.Age;

		var priorAgeSnowflake = ctx.Cfg.BirthdayConfig.AgeRoleMap.First(n => n.Age == yearsOld - 1).Role;
		var newAgeSnowflake = ctx.Cfg.BirthdayConfig.AgeRoleMap.First(n => n.Age == yearsOld).Role;

		// Preassert
		var user = ctx.Discord.GetUser(ctx.TestUserId);
		user.Should().NotBeNull();
		user.RoleIds.Should().Contain(priorAgeSnowflake);
		user.RoleIds.Should().NotContain(newAgeSnowflake);


		// Act
		await ctx.System.RunAsync();

		// Assert
		// The main thing we're looking for in this test is whether the previous age
		// role was removed and the new one applied.
		user = ctx.Discord.GetUser(ctx.TestUserId);
		user.Should().NotBeNull();
		user.RoleIds.Should().NotContain(priorAgeSnowflake);
		user.RoleIds.Should().Contain(newAgeSnowflake);
	}

	[Fact]
	public static async Task BirthdaySystem_WhenUserBirthdayWithNoYear_ShouldNotUpdateAgeRoles()
	{
		// Arrange
		var birthday = DateTime.Parse("1600-02-14T00:00:00Z");
		var today = DateTime.Parse("2016-02-14T00:00:00Z");

		var ctx = await Setup(today, birthday, roleUpdateFn: (_, cfg) =>
		{
			// just return the 16 age role
			return [ cfg.BirthdayConfig.AgeRoleMap.First(n => n.Age == 16).Role.ID ];
		});

		// Preassert
		var user = ctx.Discord.GetUser(ctx.TestUserId);
		user.Should().NotBeNull();

		user.RoleIds.Should().ContainSingle();
		var priorRoleId = user.RoleIds.First();

		// Act
		await ctx.System.RunAsync();

		// Assert
		user = ctx.Discord.GetUser(ctx.TestUserId);
		user.Should().NotBeNull();
		// Since the user's birth year isn't set, we can't actually calculate their age,
		// so we expect that their age roles won't be changed.
		user.RoleIds.Should().HaveCount(2);
		user.RoleIds.Should().Contain(ctx.Cfg.BirthdayConfig.BirthdayRole);
		user.RoleIds.Should().Contain(priorRoleId);
	}

	[Fact]
	public static async Task BirthdaySystem_WhenNoBirthdays_ShouldDoNothing()
	{
		// Arrange
		var birthday = DateTime.Parse("2000-02-14T00:00:00Z");
		var today = DateTime.Parse("2025-02-17T00:00:00Z");

		var ctx = await Setup(today, birthday);

		// Act
		await ctx.System.RunAsync();

		// Assert
		var user = ctx.Discord.GetUser(ctx.TestUserId);
		user.Should().NotBeNull();
		user.RoleIds.Should().NotContain(ctx.Cfg.BirthdayConfig.BirthdayRole);

		var channel = await ctx.Discord.GetChannel(ctx.Cfg.BirthdayConfig.BirthdayAnnounceChannel) as TestChannel;
		channel.Should().NotBeNull();

		var messages = await channel.GetMessagesAsync().SelectMany(n => n).ToListAsync();
		messages.Count.Should().Be(0);
	}

	[Fact]
	public static async Task BirthdaySystem_WithUserHavingOldBirthday_ShouldRemoveOldBirthdayRoles()
	{
		// Arrange
		var birthday = DateTime.Parse("2000-02-13T00:00:00Z");
		var today = DateTime.Parse("2025-02-14T00:00:00Z");

		var ctx = await Setup(today, birthday, true);

		// Pre assert
		var user = ctx.Discord.GetUser(ctx.TestUserId);
		user.Should().NotBeNull();
		user.RoleIds.Should().Contain(ctx.Cfg.BirthdayConfig.BirthdayRole);


		// Act
		await ctx.System.RunAsync();

		// Assert
		user = ctx.Discord.GetUser(ctx.TestUserId);
		user.Should().NotBeNull();
		user.RoleIds.Should().NotContain(ctx.Cfg.BirthdayConfig.BirthdayRole);
	}

	[Fact]
	public static async Task BirthdaySystem_WithBirthdayRoleButNoBirthdayRecord_ShouldRemoveBirthdayRoles()
	{
		// Arrange
		var birthday = DateTime.Parse("2000-02-13T00:00:00Z");
		var today = DateTime.Parse("2025-02-14T00:00:00Z");

		var ctx = await Setup(today, applyBirthday: true);

		// Pre assert
		var user = ctx.Discord.GetUser(ctx.TestUserId);
		user.Should().NotBeNull();
		user.RoleIds.Should().Contain(ctx.Cfg.BirthdayConfig.BirthdayRole);


		// Act
		await ctx.System.RunAsync();

		// Assert
		user = ctx.Discord.GetUser(ctx.TestUserId);
		user.Should().NotBeNull();
		user.RoleIds.Should().NotContain(ctx.Cfg.BirthdayConfig.BirthdayRole);
	}

	[Fact]
	public static async Task BirthdaySystem_WithUserBirthdayStill_ShouldKeepBirthdayRoles()
	{
		// Arrange
		var birthday = DateTime.Parse("2000-02-13T12:00:00Z");
		var today = DateTime.Parse("2025-02-14T00:00:00Z");

		var ctx = await Setup(today, birthday, true);

		// Pre assert
		var user = ctx.Discord.GetUser(ctx.TestUserId);
		user.Should().NotBeNull();
		user.RoleIds.Should().Contain(ctx.Cfg.BirthdayConfig.BirthdayRole);

		// Act
		await ctx.System.RunAsync();

		// Assert
		user = ctx.Discord.GetUser(ctx.TestUserId);
		user.Should().NotBeNull();
		user.RoleIds.Should().Contain(ctx.Cfg.BirthdayConfig.BirthdayRole);
	}

	private record Context(
		Snowflake TestUserId,
		BirthdaySystem System,
		MockInstarDDBService DDB,
		MockDiscordService Discord,
		MockMetricService Metrics,
		InstarDynamicConfiguration Cfg,
		Birthday? Birthday
		);
}