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

namespace InstarBot.Tests.Integration.Interactions;

public static class SetBirthdayCommandTests
{
    private static async Task<(IInstarDDBService, Mock<SetBirthdayCommand>, InstarDynamicConfiguration)> SetupMocks(SetBirthdayContext context, DateTime? timeOverride = null, bool throwError = false)
    {
        TestUtilities.SetupLogging();

		var timeProvider = TimeProvider.System;
		if (timeOverride is not null)
		{
			var timeProviderMock = new Mock<TimeProvider>();
			timeProviderMock.Setup(n => n.GetUtcNow()).Returns(new DateTimeOffset((DateTime)timeOverride));
			timeProvider = timeProviderMock.Object;
		}

		var staffAnnounceChannelMock = new Mock<ITextChannel>();
		var birthdayAnnounceChannelMock = new Mock<ITextChannel>();
		context.StaffAnnounceChannel = staffAnnounceChannelMock;
		context.BirthdayAnnounceChannel = birthdayAnnounceChannelMock;

		var ddbService = TestUtilities.GetServices().GetService<IInstarDDBService>();
		var cfgService = TestUtilities.GetDynamicConfiguration();
		var cfg = await cfgService.GetConfig();

		var guildMock = new Mock<IInstarGuild>();
		guildMock.Setup(n => n.GetTextChannel(cfg.StaffAnnounceChannel)).Returns(staffAnnounceChannelMock.Object);
		guildMock.Setup(n => n.GetTextChannel(cfg.BirthdayConfig.BirthdayAnnounceChannel)).Returns(birthdayAnnounceChannelMock.Object);

		var testContext = new TestContext
		{
			UserID = context.UserID.ID,
			Channels =
			{
				{ cfg.StaffAnnounceChannel, staffAnnounceChannelMock.Object },
				{ cfg.BirthdayConfig.BirthdayAnnounceChannel, birthdayAnnounceChannelMock.Object }
			}
		};

		var discord = TestUtilities.SetupDiscordService(testContext);
		if (discord is MockDiscordService mockDiscord)
		{
			mockDiscord.Guild = guildMock.Object;
		}

		var birthdaySystem = new BirthdaySystem(cfgService, discord, ddbService, new MockMetricService(), timeProvider);

		if (throwError && ddbService is MockInstarDDBService mockDDB)
			mockDDB.Setup(n => n.GetOrCreateUserAsync(It.IsAny<IGuildUser>())).Throws<BadStateException>();

		var cmd = TestUtilities.SetupCommandMock(() => new SetBirthdayCommand(ddbService, TestUtilities.GetDynamicConfiguration(), new MockMetricService(), birthdaySystem, timeProvider), testContext);

		await cmd.Object.Context.User!.AddRoleAsync(cfg.NewMemberRoleID);

		cmd.Setup(n => n.Context.User!.GuildId).Returns(TestUtilities.GuildID);
		
		context.User = cmd.Object.Context.User!;

		cmd.Setup(n => n.Context.Guild).Returns(guildMock.Object);

		((MockInstarDDBService) ddbService).Register(InstarUserData.CreateFrom(cmd.Object.Context.User!));

		ddbService.Should().NotBeNull();

        return (ddbService, cmd, cfg);
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
        var context = new SetBirthdayContext(Snowflake.Generate(), year, month, day, timezone);

        var (ddb, cmd, _) = await SetupMocks(context);

        // Act
        await cmd.Object.SetBirthday((Month)context.Month, context.Day, context.Year, context.TimeZone);

        // Assert
        var date = context.ToDateTime();
        
        var ddbUser = await ddb.GetUserAsync(context.UserID.ID);
		ddbUser!.Data.Birthday.Should().NotBeNull();
		ddbUser.Data.Birthday.Birthdate.UtcDateTime.Should().Be(date.UtcDateTime);
		ddbUser.Data.Birthdate.Should().Be(date.UtcDateTime.ToString("MMddHHmm"));
        TestUtilities.VerifyMessage(cmd, Strings.Command_SetBirthday_Success, true);
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
        var context = new SetBirthdayContext(Snowflake.Generate(), year, month, day);

        var (_, cmd, _) = await SetupMocks(context);

        // Act
        await cmd.Object.SetBirthday((Month)context.Month, context.Day, context.Year, context.TimeZone);

        // Assert
        if (month is < 0 or > 12)
        {
            TestUtilities.VerifyMessage(cmd,
                Strings.Command_SetBirthday_MonthsOutOfRange, true);
        }
        else
        {
            var date = new DateTime(context.Year, context.Month, 1); // there's always a 1st of the month
            var daysInMonth = DateTime.DaysInMonth(context.Year, context.Month);

            // Assert
            TestUtilities.VerifyMessage(cmd,
                Strings.Command_SetBirthday_DaysInMonthOutOfRange, true);
        }
	}

	[Fact(DisplayName = "Attempting to set a birthday in the future should emit an error message.")]
	public static async Task SetBirthdayCommand_WithDateInFuture_ShouldReturnError()
	{
		// Arrange
		// Note: Update this in the year 9,999
		var context = new SetBirthdayContext(Snowflake.Generate(), 9999, 1, 1);

		var (_, cmd, _) = await SetupMocks(context);

		// Act
		await cmd.Object.SetBirthday((Month) context.Month, context.Day, context.Year, context.TimeZone);

		// Assert
		TestUtilities.VerifyMessage(cmd, Strings.Command_SetBirthday_NotTimeTraveler, true);
	}

	[Fact(DisplayName = "Attempting to set a birthday when user has already set one should emit an error message.")]
	public static async Task SetBirthdayCommand_BirthdayAlreadyExists_ShouldReturnError()
	{
		// Arrange
		// Note: Update this in the year 9,999
		var context = new SetBirthdayContext(Snowflake.Generate(), 2000, 1, 1);

		var (ddb, cmd, _) = await SetupMocks(context);

		var dbUser = await ddb.GetOrCreateUserAsync(context.User);
		dbUser.Data.Birthday = new Birthday(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc, TimeProvider.System);
		dbUser.Data.Birthdate = dbUser.Data.Birthday.Key;
		await dbUser.UpdateAsync();


		// Act
		await cmd.Object.SetBirthday((Month) context.Month, context.Day, context.Year, context.TimeZone);

		// Assert
		TestUtilities.VerifyMessage(cmd, Strings.Command_SetBirthday_Error_AlreadySet, true);
	}

	[Fact(DisplayName = "An exception should return a message.")]
	public static async Task SetBirthdayCommand_WithException_ShouldPromptUserToTryAgainLater()
	{
		// Arrange
		// Note: Update this in the year 9,999
		var context = new SetBirthdayContext(Snowflake.Generate(), 2000, 1, 1);

		var (_, cmd, _) = await SetupMocks(context, throwError: true);

		// Act
		await cmd.Object.SetBirthday((Month) context.Month, context.Day, context.Year, context.TimeZone);

		// Assert
		TestUtilities.VerifyMessage(cmd, Strings.Command_SetBirthday_Error_CouldNotSetBirthday, true);
	}

	[Fact(DisplayName = "Attempting to set an underage birthday should result in an AMH and staff notification.")]
	public static async Task SetBirthdayCommand_WithUnderage_ShouldNotifyStaff()
	{
		// Arrange
		// Note: Update this in the year 9,999
		var context = new SetBirthdayContext(Snowflake.Generate(), 2000, 1, 1);

		var (ddb, cmd, cfg) = await SetupMocks(context, new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc));

		// Act
		await cmd.Object.SetBirthday((Month) context.Month, context.Day, context.Year, context.TimeZone);

		// Assert
		var date = context.ToDateTime();

		var ddbUser = await ddb.GetUserAsync(context.UserID.ID);
		ddbUser!.Data.Birthday.Should().NotBeNull();
		ddbUser.Data.Birthday.Birthdate.UtcDateTime.Should().Be(date.UtcDateTime);
		ddbUser.Data.Birthdate.Should().Be(date.UtcDateTime.ToString("MMddHHmm"));

		ddbUser.Data.AutoMemberHoldRecord.Should().NotBeNull();
		ddbUser.Data.AutoMemberHoldRecord!.ModeratorID.Should().Be(cfg.BotUserID);

		TestUtilities.VerifyMessage(cmd, Strings.Command_SetBirthday_Success, true);

		var staffAnnounceChannel = cmd.Object.Context.Guild.GetTextChannel(cfg.StaffAnnounceChannel);
		staffAnnounceChannel.Should().NotBeNull();


		// Verify embed
		var embedVerifier = EmbedVerifier.Builder()
			.WithDescription(Strings.Embed_UnderageUser_WarningTemplate_NewMember).Build();

		TestUtilities.VerifyChannelEmbed(context.StaffAnnounceChannel, embedVerifier, $"<@&{cfg.StaffRoleID}>");
	}

	[Fact(DisplayName = "Attempting to set a birthday to today should grant the birthday role.")]
	public static async Task SetBirthdayCommand_BirthdayIsToday_ShouldGrantBirthdayRoles()
	{
		// Arrange
		// Note: Update this in the year 9,999
		var context = new SetBirthdayContext(Snowflake.Generate(), 2000, 1, 1);

		var (ddb, cmd, cfg) = await SetupMocks(context, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

		// Act
		await cmd.Object.SetBirthday((Month) context.Month, context.Day, context.Year, context.TimeZone);

		// Assert
		var date = context.ToDateTime();

		context.User.RoleIds.Should().Contain(cfg.BirthdayConfig.BirthdayRole);

		var ddbUser = await ddb.GetUserAsync(context.UserID.ID);
		ddbUser!.Data.Birthday.Should().NotBeNull();
		ddbUser.Data.Birthday.Birthdate.UtcDateTime.Should().Be(date.UtcDateTime);
		ddbUser.Data.Birthdate.Should().Be(date.UtcDateTime.ToString("MMddHHmm"));

		ddbUser.Data.AutoMemberHoldRecord.Should().BeNull();

		TestUtilities.VerifyMessage(cmd, Strings.Command_SetBirthday_Success, true);

		var birthdayAnnounceChannel = cmd.Object.Context.Guild.GetTextChannel(cfg.BirthdayConfig.BirthdayAnnounceChannel);
		birthdayAnnounceChannel.Should().NotBeNull();
		
		TestUtilities.VerifyChannelMessage(context.BirthdayAnnounceChannel, Strings.Birthday_Announcement);
	}

	private record SetBirthdayContext(Snowflake UserID, int Year, int Month, int Day, int TimeZone = 0)
    {
        public DateTimeOffset ToDateTime()
        {
            var unspecifiedDate = new DateTime(Year, Month, Day, 0, 0, 0, DateTimeKind.Unspecified);
            var timeZone = new DateTimeOffset(unspecifiedDate, TimeSpan.FromHours(TimeZone));

            return timeZone;
		}

		public Mock<ITextChannel> StaffAnnounceChannel { get; set; }
		public IGuildUser User { get; set; }
		public Mock<ITextChannel> BirthdayAnnounceChannel { get ; set ; }
    }
}