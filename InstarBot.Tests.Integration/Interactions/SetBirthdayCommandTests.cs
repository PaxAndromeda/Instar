using FluentAssertions;
using InstarBot.Tests.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Services;
using Xunit;

namespace InstarBot.Tests.Integration.Interactions;

public static class SetBirthdayCommandTests
{
    private static (IInstarDDBService, Mock<SetBirthdayCommand>) SetupMocks(SetBirthdayContext context)
    {
        TestUtilities.SetupLogging();
        
        var ddbService = TestUtilities.GetServices().GetService<IInstarDDBService>();
        var cmd = TestUtilities.SetupCommandMock(() => new SetBirthdayCommand(ddbService!, new MockMetricService()), new TestContext
        {
            UserID = context.User.ID
        });
        
        ((MockInstarDDBService) ddbService!).Register(InstarUserData.CreateFrom(cmd.Object.Context.User!));

		ddbService.Should().NotBeNull();

        return (ddbService, cmd);
    }


    [Theory(DisplayName = "User should be able to set their birthday when providing a valid date.")]
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

        var (ddb, cmd) = SetupMocks(context);

        // Act
        await cmd.Object.SetBirthday((Month)context.Month, context.Day, context.Year, context.TimeZone);

        // Assert
        var date = context.ToDateTime();
        
        var ddbUser = await ddb.GetUserAsync(context.User.ID);
        ddbUser!.Data.Birthday.Should().Be(date.UtcDateTime);
        TestUtilities.VerifyMessage(cmd, $"Your birthday was set to {date.DateTime:dddd, MMMM d, yyy}.", true);
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

        var (_, cmd) = SetupMocks(context);

        // Act
        await cmd.Object.SetBirthday((Month)context.Month, context.Day, context.Year, context.TimeZone);

        // Assert
        if (month is < 0 or > 12)
        {
            TestUtilities.VerifyMessage(cmd,
                "There are only 12 months in a year.  Your birthday was not set.", true);
        }
        else
        {
            var date = new DateTime(context.Year, context.Month, 1); // there's always a 1st of the month
            var daysInMonth = DateTime.DaysInMonth(context.Year, context.Month);

            // Assert
            TestUtilities.VerifyMessage(cmd,
                $"There are only {daysInMonth} days in {date:MMMM yyy}.  Your birthday was not set.", true);
        }
    }

    [Fact(DisplayName = "Attempting to set a birthday in the future should emit an error message.")]
    public static async Task SetBirthdayCommand_WithDateInFuture_ShouldReturnError()
    {
        // Arrange
        // Note: Update this in the year 9,999
        var context = new SetBirthdayContext(Snowflake.Generate(), 9999, 1, 1);

        var (_, cmd) = SetupMocks(context);

        // Act
        await cmd.Object.SetBirthday((Month)context.Month, context.Day, context.Year, context.TimeZone);

        // Assert
        TestUtilities.VerifyMessage(cmd, "You are not a time traveler.  Your birthday was not set.", true);
    }

    private record SetBirthdayContext(Snowflake User, int Year, int Month, int Day, int TimeZone = 0)
    {
        public DateTimeOffset ToDateTime()
        {
            var unspecifiedDate = new DateTime(Year, Month, Day, 0, 0, 0, DateTimeKind.Unspecified);
            var timeZone = new DateTimeOffset(unspecifiedDate, TimeSpan.FromHours(TimeZone));

            return timeZone;
        }
    }
}