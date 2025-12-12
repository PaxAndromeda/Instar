using Discord;
using FluentAssertions;
using InstarBot.Tests.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Services;
using Xunit;
using Assert = Xunit.Assert;

namespace InstarBot.Tests.Integration.Interactions;

public static class ResetBirthdayCommandTests
{
	private static async Task<(IInstarDDBService, Mock<ResetBirthdayCommand>, IGuildUser, TestContext, InstarDynamicConfiguration cfg)> SetupMocks(Birthday? userBirthday = null, bool throwError = false, bool skipDbInsert = false)
	{
		TestUtilities.SetupLogging();

		var ddbService = TestUtilities.GetServices().GetService<IInstarDDBService>();
		var cfgService = TestUtilities.GetDynamicConfiguration();
		var cfg = await cfgService.GetConfig();
		var userId = Snowflake.Generate();

		if (throwError && ddbService is MockInstarDDBService mockDDB)
		{
			mockDDB.Setup(n => n.GetUserAsync(It.IsAny<Snowflake>())).Throws<BadStateException>();

			// assert that we're actually throwing an exception
			await Assert.ThrowsAsync<BadStateException>(async () => await ddbService.GetUserAsync(userId));
		}

		var testContext = new TestContext
		{
			UserID = userId
		};

		var cmd = TestUtilities.SetupCommandMock(() => new ResetBirthdayCommand(ddbService!, cfgService), testContext);

		await cmd.Object.Context.User!.AddRoleAsync(cfg.NewMemberRoleID);

		cmd.Setup(n => n.Context.User!.GuildId).Returns(TestUtilities.GuildID);

		if (!skipDbInsert)
			((MockInstarDDBService) ddbService!).Register(InstarUserData.CreateFrom(cmd.Object.Context.User!));

		ddbService.Should().NotBeNull();

		if (userBirthday is null)
			return (ddbService, cmd, cmd.Object.Context.User!, testContext, cfg);

		var dbUser = await ddbService.GetUserAsync(userId);
		dbUser!.Data.Birthday = userBirthday;
		dbUser.Data.Birthdate = userBirthday.Key;
		await dbUser.UpdateAsync();

		return (ddbService, cmd, cmd.Object.Context.User!, testContext, cfg);
	}

	[Fact]
	public static async Task ResetBirthday_WithEligibleUser_ShouldHaveBirthdayReset()
	{
		// Arrange
		var (ddb, cmd, user, ctx, _) = await SetupMocks(userBirthday: new Birthday(new DateTime(2000, 1, 1), TimeProvider.System));

		// Act
		await cmd.Object.ResetBirthday(user);

		// Assert
		var dbUser = await ddb.GetUserAsync(user.Id);
		dbUser.Should().NotBeNull();
		dbUser.Data.Birthday.Should().BeNull();
		dbUser.Data.Birthdate.Should().BeNull();

		TestUtilities.VerifyMessage(cmd, Strings.Command_ResetBirthday_Success, ephemeral: true);
		TestUtilities.VerifyChannelMessage(ctx.DMChannelMock, Strings.Command_ResetBirthday_EndUserNotification);
	}

	[Fact]
	public static async Task ResetBirthday_UserNotFound_ShouldEmitError()
	{
		// Arrange
		var (ddb, cmd, user, ctx, _) = await SetupMocks(skipDbInsert: true);

		// Act
		await cmd.Object.ResetBirthday(user);

		// Assert
		var dbUser = await ddb.GetUserAsync(user.Id);
		dbUser.Should().BeNull();

		TestUtilities.VerifyMessage(cmd, Strings.Command_ResetBirthday_Error_UserNotFound, ephemeral: true);
	}

	[Fact]
	public static async Task ResetBirthday_UserHasBirthdayRole_ShouldRemoveRole()
	{
		// Arrange
		var (ddb, cmd, user, ctx, cfg) = await SetupMocks(userBirthday: new Birthday(new DateTime(2000, 1, 1), TimeProvider.System));

		await user.AddRoleAsync(cfg.BirthdayConfig.BirthdayRole);
		user.RoleIds.Should().Contain(cfg.BirthdayConfig.BirthdayRole);

		// Act
		await cmd.Object.ResetBirthday(user);

		// Assert
		var dbUser = await ddb.GetUserAsync(user.Id);
		dbUser.Should().NotBeNull();
		dbUser.Data.Birthday.Should().BeNull();
		dbUser.Data.Birthdate.Should().BeNull();

		user.RoleIds.Should().NotContain(cfg.BirthdayConfig.BirthdayRole);

		TestUtilities.VerifyMessage(cmd, Strings.Command_ResetBirthday_Success, ephemeral: true);
		TestUtilities.VerifyChannelMessage(ctx.DMChannelMock, Strings.Command_ResetBirthday_EndUserNotification);
	}

	[Fact]
	public static async Task ResetBirthday_WithDBError_ShouldEmitError()
	{
		// Arrange
		var (ddb, cmd, user, ctx, _) = await SetupMocks(throwError: true);

		// Act
		await cmd.Object.ResetBirthday(user);

		// Assert
		TestUtilities.VerifyMessage(cmd, Strings.Command_ResetBirthday_Error_Unknown, ephemeral: true);
	}
}