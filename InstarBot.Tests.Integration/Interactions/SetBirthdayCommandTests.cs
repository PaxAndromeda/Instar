using Discord;
using FluentAssertions;
using InstarBot.Test.Framework;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.Services;
using Xunit;

namespace InstarBot.Tests.Integration.Interactions;

public static class SetBirthdayCommandTests
{
	private static async Task<TestOrchestrator> SetupOrchestrator(bool throwError = false)
	{
		var orchestrator = TestOrchestrator.Default;
		await orchestrator.Actor.AddRoleAsync(orchestrator.Configuration.NewMemberRoleID);

		if (throwError)
		{
			if (orchestrator.Database is not IMockOf<IDatabaseService> ddbService)
				throw new InvalidOperationException("IDatabaseService was not mocked correctly.");

			ddbService.Mock.Setup(n => n.GetOrCreateUserAsync(It.IsAny<IGuildUser>())).Throws<BadStateException>();
		}

		return orchestrator;
	}


	[Theory(DisplayName = "UserID should be able to set their birthday when providing a valid date.")]
    [InlineData(1992, 7, 21, 0)]
    [InlineData(1992, 7, 21, -7)]
    [InlineData(1992, 7, 21, 7)]
    [InlineData(2000, 7, 21, 0)]
    [InlineData(2001, 12, 31, 0)]
    [InlineData(2010, 1, 1, 0)]
    public static async Task SetBirthdayCommand_WithValidDate_ShouldSetCorrectly(int year, int month, int day, int timezone)
    {
		// Arrange
		var date = new DateTimeOffset(year, month, day, 0, 0, 0, 0, TimeSpan.FromHours(timezone));

		var orchestrator = await SetupOrchestrator();
		orchestrator.SetTime(date);
		var cmd = orchestrator.GetCommand<SetBirthdayCommand>();

		// Act
		await cmd.Object.SetBirthday((Month)month, day, year, timezone);

		// Assert

		var database = orchestrator.GetService<IDatabaseService>();

        var ddbUser = await database.GetUserAsync(orchestrator.Actor.Id);
		ddbUser!.Data.Birthday.Should().NotBeNull();
		ddbUser.Data.Birthday.Birthdate.UtcDateTime.Should().Be(date.UtcDateTime);
		ddbUser.Data.Birthdate.Should().Be(date.UtcDateTime.ToString("MMddHHmm"));
        cmd.VerifyResponse(Strings.Command_SetBirthday_Success, true);
    }

    [Theory(DisplayName = "Attempting to set an invalid day or month number should emit an error message.")]
    [InlineData(1992, 13, 1)] // Invalid month
    [InlineData(1992, -7, 1)] // Invalid month
    [InlineData(1992, 1, 40)] // Invalid day
    [InlineData(1992, 2, 31)] // Leap year
    [InlineData(2028, 2, 31)] // Leap year
    [InlineData(2032, 2, 31)] // Leap year
    public static async Task SetBirthdayCommand_WithInvalidDate_ShouldReturnError(int year, int month, int day)
    {
		// Arrange
		var orchestrator = await SetupOrchestrator();
		var cmd = orchestrator.GetCommand<SetBirthdayCommand>();

		// Act
		await cmd.Object.SetBirthday((Month)month, day, year);

        // Assert
        cmd.VerifyResponse(
	        month is < 0 or > 12
		        ? Strings.Command_SetBirthday_MonthsOutOfRange
		        // Assert
		        : Strings.Command_SetBirthday_DaysInMonthOutOfRange, true);
    }

	[Fact(DisplayName = "Attempting to set a birthday in the future should emit an error message.")]
	public static async Task SetBirthdayCommand_WithDateInFuture_ShouldReturnError()
	{
		// Arrange
		var orchestrator = await SetupOrchestrator();
		var cmd = orchestrator.GetCommand<SetBirthdayCommand>();

		// Act
		await cmd.Object.SetBirthday(Month.January, 1, 9999);

		// Assert
		cmd.VerifyResponse(Strings.Command_SetBirthday_NotTimeTraveler, true);
	}

	[Fact(DisplayName = "Attempting to set a birthday when user has already set one should emit an error message.")]
	public static async Task SetBirthdayCommand_BirthdayAlreadyExists_ShouldReturnError()
	{
		// Arrange
		var orchestrator = await SetupOrchestrator();
		var cmd = orchestrator.GetCommand<SetBirthdayCommand>();

		var database = orchestrator.GetService<IDatabaseService>();
		var dbUser = await database.GetOrCreateUserAsync(orchestrator.Actor);
		dbUser.Data.Birthday = new Birthday(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc, TimeProvider.System);
		dbUser.Data.Birthdate = dbUser.Data.Birthday.Key;
		await dbUser.CommitAsync();


		// Act
		await cmd.Object.SetBirthday(Month.January, 1, 2000);

		// Assert
		cmd.VerifyResponse(Strings.Command_SetBirthday_Error_AlreadySet, true);
	}

	[Fact(DisplayName = "An exception should return a message.")]
	public static async Task SetBirthdayCommand_WithException_ShouldPromptUserToTryAgainLater()
	{
		// Arrange
		var orchestrator = await SetupOrchestrator(throwError: true);
		var cmd = orchestrator.GetCommand<SetBirthdayCommand>();

		// Act
		await cmd.Object.SetBirthday(Month.January, 1, 1);

		// Assert
		cmd.VerifyResponse(Strings.Command_SetBirthday_Error_CouldNotSetBirthday, true);
	}

	[Fact(DisplayName = "Attempting to set an underage birthday should result in an AMH and staff notification.")]
	public static async Task SetBirthdayCommand_WithUnderage_ShouldNotifyStaff()
	{
		// Arrange
		var orchestrator = await SetupOrchestrator();
		orchestrator.SetTime(new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc));
		var cmd = orchestrator.GetCommand<SetBirthdayCommand>();

		// Act
		await cmd.Object.SetBirthday(Month.January, 1, 2000);

		// Assert
		var date = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

		var ddbUser = await orchestrator.Database.GetUserAsync(orchestrator.Actor.Id);
		ddbUser!.Data.Birthday.Should().NotBeNull();
		ddbUser.Data.Birthday.Birthdate.UtcDateTime.Should().Be(date.UtcDateTime);
		ddbUser.Data.Birthdate.Should().Be(date.UtcDateTime.ToString("MMddHHmm"));

		ddbUser.Data.AutoMemberHoldRecord.Should().NotBeNull();
		ddbUser.Data.AutoMemberHoldRecord!.ModeratorID.Should().Be(orchestrator.Configuration.BotUserID);

		cmd.VerifyResponse(Strings.Command_SetBirthday_Success, true);

		var staffAnnounceChannel = cmd.Object.Context.Guild.GetTextChannel(orchestrator.Configuration.StaffAnnounceChannel);
		staffAnnounceChannel.Should().NotBeNull();


		// Verify embed
		var embedVerifier = EmbedVerifier.Builder()
			.WithDescription(Strings.Embed_UnderageUser_WarningTemplate_NewMember).Build();

		orchestrator.GetChannel(orchestrator.Configuration.StaffAnnounceChannel)
			.VerifyEmbed(embedVerifier, $"<@&{orchestrator.Configuration.StaffRoleID}>");
	}

	[Fact(DisplayName = "Attempting to set a birthday to today should grant the birthday role.")]
	public static async Task SetBirthdayCommand_BirthdayIsToday_ShouldGrantBirthdayRoles()
	{
		// Arrange
		var date = new DateTimeOffset(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));

		var orchestrator = await SetupOrchestrator();
		orchestrator.SetTime(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
		var cmd = orchestrator.GetCommand<SetBirthdayCommand>();

		// Act
		await cmd.Object.SetBirthday((Month) date.Month, date.Day, date.Year);

		// Assert
		orchestrator.Actor.RoleIds.Should().Contain(orchestrator.Configuration.BirthdayConfig.BirthdayRole);

		var ddbUser = await orchestrator.Database.GetUserAsync(orchestrator.Actor.Id);
		ddbUser!.Data.Birthday.Should().NotBeNull();
		ddbUser.Data.Birthday.Birthdate.UtcDateTime.Should().Be(date.UtcDateTime);
		ddbUser.Data.Birthdate.Should().Be(date.UtcDateTime.ToString("MMddHHmm"));

		ddbUser.Data.AutoMemberHoldRecord.Should().BeNull();

		cmd.VerifyResponse(Strings.Command_SetBirthday_Success, true);

		var birthdayAnnounceChannel = cmd.Object.Context.Guild.GetTextChannel(orchestrator.Configuration.BirthdayConfig.BirthdayAnnounceChannel);
		birthdayAnnounceChannel.Should().NotBeNull();

		orchestrator.GetChannel(orchestrator.Configuration.BirthdayConfig.BirthdayAnnounceChannel)
			.VerifyMessage(Strings.Birthday_Announcement);
	}
}