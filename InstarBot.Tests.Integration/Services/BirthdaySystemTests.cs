using FluentAssertions;
using InstarBot.Test.Framework;
using InstarBot.Test.Framework.Models;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.Services;
using Xunit;
using Metric = PaxAndromeda.Instar.Metrics.Metric;

namespace InstarBot.Tests.Integration.Services;

public static class BirthdaySystemTests
{
	private static async Task<TestOrchestrator> SetupOrchestrator(DateTimeOffset currentTime, DateTimeOffset? birthdate = null)
	{
		var orchestrator = await TestOrchestrator.Builder
			.WithTime(currentTime)
			.WithService<IBirthdaySystem, BirthdaySystem>()
			.Build();

		if (birthdate is not null)
		{
			var dbUser = await orchestrator.Database.GetOrCreateUserAsync(orchestrator.Subject);
			dbUser.Data.Birthday = new Birthday((DateTimeOffset) birthdate, orchestrator.TimeProvider);
			dbUser.Data.Birthdate = dbUser.Data.Birthday.Key;
			await dbUser.CommitAsync();

			int priorYearsOld = dbUser.Data.Birthday.Age - 1;
			var ageRole = orchestrator.Configuration.BirthdayConfig.AgeRoleMap
				.OrderByDescending(n => n.Age)
				.SkipWhile(n => n.Age > priorYearsOld)
				.First();

			await orchestrator.Subject.AddRoleAsync(ageRole.Role.ID);

			if (dbUser.Data.Birthday.IsToday)
			{
				var mockDDB = (IMockOf<IDatabaseService>) orchestrator.Database;
				mockDDB.Mock.Setup(n => n.GetUsersByBirthday(currentTime.UtcDateTime, It.IsAny<TimeSpan>()))
					.ReturnsAsync([dbUser]);
			}
		}

		await ((BirthdaySystem) orchestrator.GetService<IBirthdaySystem>()).Initialize();

		return orchestrator;
	}

	[Fact]
	public static async Task BirthdaySystem_WhenUserBirthday_ShouldGrantRole()
	{
		// Arrange
		var birthday = DateTime.Parse("2000-02-14T00:00:00Z");
		var today = DateTime.Parse("2025-02-14T00:00:00Z");

		var orchestrator = await SetupOrchestrator(today, birthday);
		var system = orchestrator.GetService<IBirthdaySystem>();

		// Act
		await system.RunAsync();

		// Assert
		// We expect a few things here: the test user should now have the birthday
		// role, and there should now be a message in the birthday announce channel.
		orchestrator.Subject.RoleIds.Should().Contain(orchestrator.Configuration.BirthdayConfig.BirthdayRole);
		orchestrator.Subject.RoleIds.Should().Contain(orchestrator.Configuration.BirthdayConfig.AgeRoleMap.MaxBy(n => n.Age)!.Role.ID);

		var channel = await orchestrator.Discord.GetChannel(orchestrator.Configuration.BirthdayConfig.BirthdayAnnounceChannel) as TestChannel;
		channel.Should().NotBeNull();

		var messages = await channel.GetMessagesAsync().SelectMany(n => n).ToListAsync();
		messages.Count.Should().BeGreaterThan(0);
		TestUtilities.MatchesFormat(Strings.Birthday_Announcement, messages[0].Content);
		
		orchestrator.Metrics.GetMetricValues(Metric.BirthdaySystem_Failures).Sum().Should().Be(0);
		orchestrator.Metrics.GetMetricValues(Metric.BirthdaySystem_Grants).Sum().Should().Be(1);
	}

	[Fact]
	public static async Task BirthdaySystem_WhenUserBirthday_ShouldUpdateAgeRoles()
	{
		// Arrange
		var birthdate = DateTime.Parse("2000-02-14T00:00:00Z");
		var today = DateTime.Parse("2016-02-14T00:00:00Z");

		var orchestrator = await SetupOrchestrator(today, birthdate);
		var system = orchestrator.GetService<IBirthdaySystem>();

		int yearsOld = new Birthday(birthdate, orchestrator.TimeProvider).Age;

		var priorAgeSnowflake = orchestrator.Configuration.BirthdayConfig.AgeRoleMap.First(n => n.Age == yearsOld - 1).Role;
		var newAgeSnowflake = orchestrator.Configuration.BirthdayConfig.AgeRoleMap.First(n => n.Age == yearsOld).Role;

		// Preassert
		orchestrator.Subject.RoleIds.Should().Contain(priorAgeSnowflake);
		orchestrator.Subject.RoleIds.Should().NotContain(newAgeSnowflake);


		// Act
		await system.RunAsync();

		// Assert
		// The main thing we're looking for in this test is whether the previous age
		// role was removed and the new one applied.
		orchestrator.Subject.RoleIds.Should().NotContain(priorAgeSnowflake);
		orchestrator.Subject.RoleIds.Should().Contain(newAgeSnowflake);
	}

	[Fact]
	public static async Task BirthdaySystem_WhenUserBirthdayWithNoYear_ShouldNotUpdateAgeRoles()
	{
		// Arrange
		var birthday = DateTime.Parse("1600-02-14T00:00:00Z");
		var today = DateTime.Parse("2016-02-14T00:00:00Z");

		var orchestrator = await SetupOrchestrator(today, birthday);
		await orchestrator.Subject.RemoveRolesAsync(orchestrator.Subject.RoleIds);
		await orchestrator.Subject.AddRoleAsync(orchestrator.Configuration.BirthdayConfig.AgeRoleMap.First(n => n.Age == 16).Role.ID);

		var system = orchestrator.GetService<IBirthdaySystem>();

		// Preassert
		orchestrator.Subject.RoleIds.Should().ContainSingle();
		var priorRoleId = orchestrator.Subject.RoleIds.First();

		// Act
		await system.RunAsync();

		// Assert
		// Since the user's birth year isn't set, we can't actually calculate their age,
		// so we expect that their age roles won't be changed.
		orchestrator.Subject.RoleIds.Should().HaveCount(2);
		orchestrator.Subject.RoleIds.Should().Contain(orchestrator.Configuration.BirthdayConfig.BirthdayRole);
		orchestrator.Subject.RoleIds.Should().Contain(priorRoleId);
	}

	[Fact]
	public static async Task BirthdaySystem_WhenNoBirthdays_ShouldDoNothing()
	{
		// Arrange
		var birthday = DateTime.Parse("2000-02-14T00:00:00Z");
		var today = DateTime.Parse("2025-02-17T00:00:00Z");

		var orchestrator = await SetupOrchestrator(today, birthday);
		var system = orchestrator.GetService<IBirthdaySystem>();

		// Act
		await system.RunAsync();

		// Assert
		orchestrator.Subject.RoleIds.Should().NotContain(orchestrator.Configuration.BirthdayConfig.BirthdayRole);

		var channel = await orchestrator.Discord.GetChannel(orchestrator.Configuration.BirthdayConfig.BirthdayAnnounceChannel) as TestChannel;
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

		var orchestrator = await SetupOrchestrator(today, birthday);
		var system = orchestrator.GetService<IBirthdaySystem>();

		// Add the birthday role
		await orchestrator.Subject.AddRoleAsync(orchestrator.Configuration.BirthdayConfig.BirthdayRole);

		// Pre assert
		orchestrator.Subject.RoleIds.Should().Contain(orchestrator.Configuration.BirthdayConfig.BirthdayRole);


		// Act
		await system.RunAsync();

		// Assert
		orchestrator.Subject.RoleIds.Should().NotContain(orchestrator.Configuration.BirthdayConfig.BirthdayRole);
	}

	[Fact]
	public static async Task BirthdaySystem_WithBirthdayRoleButNoBirthdayRecord_ShouldRemoveBirthdayRoles()
	{
		// Arrange
		var birthday = DateTime.Parse("2000-02-13T00:00:00Z");
		var today = DateTime.Parse("2025-02-14T00:00:00Z");

		var orchestrator = await SetupOrchestrator(today, birthday);
		var system = orchestrator.GetService<IBirthdaySystem>();

		// Add the birthday role
		await orchestrator.Subject.AddRoleAsync(orchestrator.Configuration.BirthdayConfig.BirthdayRole);


		// Pre assert
		orchestrator.Subject.RoleIds.Should().Contain(orchestrator.Configuration.BirthdayConfig.BirthdayRole);


		// Act
		await system.RunAsync();

		// Assert
		orchestrator.Subject.RoleIds.Should().NotContain(orchestrator.Configuration.BirthdayConfig.BirthdayRole);
	}

	[Fact]
	public static async Task BirthdaySystem_WithUserBirthdayStill_ShouldKeepBirthdayRoles()
	{
		// Arrange
		var birthday = DateTime.Parse("2000-02-13T12:00:00Z");
		var today = DateTime.Parse("2025-02-14T00:00:00Z");

		var orchestrator = await SetupOrchestrator(today, birthday);
		var system = orchestrator.GetService<IBirthdaySystem>();

		// Add the birthday role
		await orchestrator.Subject.AddRoleAsync(orchestrator.Configuration.BirthdayConfig.BirthdayRole);

		// Pre assert
		orchestrator.Subject.RoleIds.Should().Contain(orchestrator.Configuration.BirthdayConfig.BirthdayRole);

		// Act
		await system.RunAsync();

		// Assert
		orchestrator.Subject.RoleIds.Should().Contain(orchestrator.Configuration.BirthdayConfig.BirthdayRole);
	}
}