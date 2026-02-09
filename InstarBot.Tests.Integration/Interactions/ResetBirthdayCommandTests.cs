using FluentAssertions;
using InstarBot.Test.Framework;
using InstarBot.Test.Framework.Services;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.DynamoModels;
using Xunit;
using Assert = Xunit.Assert;

namespace InstarBot.Tests.Integration.Interactions;

public static class ResetBirthdayCommandTests
{
	private static async Task<TestOrchestrator> SetupOrchestrator(DateTimeOffset? userBirthday = null, bool throwsError = false)
	{
		var orchestrator = TestOrchestrator.Default;

		await orchestrator.Actor.AddRoleAsync(orchestrator.Configuration.NewMemberRoleID);

		if (throwsError && orchestrator.Database is TestDatabaseService tds)
		{
			tds.Mock.Setup(n => n.GetUserAsync(It.IsAny<Snowflake>())).Throws<BadStateException>();

			// assert that we're actually throwing an exception
			await Assert.ThrowsAsync<BadStateException>(async () => await tds.GetUserAsync(orchestrator.Actor.Id));
		}

		orchestrator.Subject = orchestrator.CreateUser();
		if (userBirthday is null) 
			return orchestrator;

		var dbEntry = InstarUserData.CreateFrom(orchestrator.Subject);
		dbEntry.Birthday = new Birthday((DateTimeOffset) userBirthday, orchestrator.TimeProvider);
		dbEntry.Birthdate = dbEntry.Birthday.Key;

		await orchestrator.Database.CreateUserAsync(dbEntry);

		return orchestrator;
	}

	[Fact]
	public static async Task ResetBirthday_WithEligibleUser_ShouldHaveBirthdayReset()
	{
		// Arrange
		var orchestrator = await SetupOrchestrator(new DateTime(2000, 1, 1));
		var cmd = orchestrator.GetCommand<ResetBirthdayCommand>();


		// Act
		await cmd.Object.ResetBirthday(orchestrator.Subject);

		// Assert
		var dbUser = await orchestrator.Database.GetUserAsync(orchestrator.Subject.Id);
		dbUser.Should().NotBeNull();
		dbUser.Data.Birthday.Should().BeNull();
		dbUser.Data.Birthdate.Should().BeNull();

		cmd.VerifyResponse(Strings.Command_ResetBirthday_Success, ephemeral: true);
		orchestrator.Subject.DMChannelMock.VerifyMessage(Strings.Command_ResetBirthday_EndUserNotification);
	}

	[Fact]
	public static async Task ResetBirthday_UserNotFound_ShouldEmitError()
	{
		// Arrange
		var orchestrator = await SetupOrchestrator();
		var cmd = orchestrator.GetCommand<ResetBirthdayCommand>();

		// Act
		await cmd.Object.ResetBirthday(orchestrator.Subject);

		// Assert
		var dbUser = await orchestrator.Database.GetUserAsync(orchestrator.Subject.Id);
		dbUser.Should().BeNull();

		cmd.VerifyResponse(Strings.Command_ResetBirthday_Error_UserNotFound, ephemeral: true);
	}

	[Fact]
	public static async Task ResetBirthday_UserHasBirthdayRole_ShouldRemoveRole()
	{
		// Arrange
		var orchestrator = await SetupOrchestrator(new DateTime(2000, 1, 1));
		var birthdayRole = orchestrator.Configuration.BirthdayConfig.BirthdayRole;

		var cmd = orchestrator.GetCommand<ResetBirthdayCommand>();

		await orchestrator.Subject.AddRoleAsync(birthdayRole);

		orchestrator.Subject.RoleIds.Should().Contain(birthdayRole);

		// Act
		await cmd.Object.ResetBirthday(orchestrator.Subject);

		// Assert
		var dbUser = await orchestrator.Database.GetUserAsync(orchestrator.Subject.Id);
		dbUser.Should().NotBeNull();
		dbUser.Data.Birthday.Should().BeNull();
		dbUser.Data.Birthdate.Should().BeNull();

		orchestrator.Subject.RoleIds.Should().NotContain(birthdayRole);

		cmd.VerifyResponse(Strings.Command_ResetBirthday_Success, ephemeral: true);
		orchestrator.Subject.DMChannelMock.VerifyMessage(Strings.Command_ResetBirthday_EndUserNotification);
	}

	[Fact]
	public static async Task ResetBirthday_WithDBError_ShouldEmitError()
	{
		// Arrange
		var orchestrator = await SetupOrchestrator(throwsError: true);
		var cmd = orchestrator.GetCommand<ResetBirthdayCommand>();

		// Act
		await cmd.Object.ResetBirthday(orchestrator.Subject);

		// Assert
		cmd.VerifyResponse(Strings.Command_ResetBirthday_Error_Unknown, ephemeral: true);
	}
}