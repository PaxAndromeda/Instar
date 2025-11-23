using Discord;
using InstarBot.Tests.Services;
using Moq;
using Moq.Protected;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using Xunit;

namespace InstarBot.Tests.Integration.Interactions;

public static class PageCommandTests
{
    private static async Task<Mock<PageCommand>> SetupCommandMock(PageCommandTestContext context)
    {
        // Treat the Test page target as a regular non-staff user on the server
        var userTeam = context.UserTeamID == PageTarget.Test
            ? Snowflake.Generate()
            : (await TestUtilities.GetTeams(context.UserTeamID).FirstAsync()).ID;

        var commandMock = TestUtilities.SetupCommandMock(
            () => new PageCommand(TestUtilities.GetTeamService(), new MockMetricService()),
            new TestContext
            {
                UserRoles = [userTeam]
            });

        return commandMock;
    }

    private static async Task<ulong> GetTeamLead(PageTarget pageTarget)
    {
        var dynamicConfig = await TestUtilities.GetDynamicConfiguration().GetConfig();

        var teamsConfig =
            dynamicConfig.Teams.ToDictionary(n => n.InternalID, n => n);

        // Eeeeeeeeeeeeevil
        return teamsConfig[pageTarget.GetAttributesOfType<TeamRefAttribute>()?.First().InternalID ?? "idkjustfail"]
            .Teamleader;
    }

    private static async Task VerifyPageEmbedEmitted(Mock<PageCommand> command, PageCommandTestContext context)
    {
        var pageTarget = context.PageTarget;

        string expectedString;

        if (context.PagingTeamLeader)
            expectedString = $"<@{await GetTeamLead(pageTarget)}>";
        else
            switch (pageTarget)
            {
                case PageTarget.All:
                    expectedString = string.Join(' ',
                        await TestUtilities.GetTeams(PageTarget.All).Select(n => Snowflake.GetMention(() => n.ID))
                            .ToArrayAsync());
                    break;
                case PageTarget.Test:
                    expectedString = "This is a __**TEST**__ page.";
                    break;
                default:
                    var team = await TestUtilities.GetTeams(pageTarget).FirstAsync();
                    expectedString = Snowflake.GetMention(() => team.ID);
                    break;
            }

        command.Protected().Verify(
            "RespondAsync", Times.Once(),
            expectedString, ItExpr.IsNull<Embed[]>(),
            false, false, AllowedMentions.All, ItExpr.IsNull<RequestOptions>(),
            ItExpr.IsNull<MessageComponent>(), ItExpr.IsAny<Embed>(), ItExpr.IsAny<PollProperties>(), ItExpr.IsAny<MessageFlags>());
    }

    [Fact(DisplayName = "User should be able to page when authorized")]
    public static async Task PageCommand_Authorized_WhenPagingTeam_ShouldPageCorrectly()
    {
        // Arrange
        var context = new PageCommandTestContext(
            PageTarget.Owner,
            PageTarget.Moderator,
            false
        );

        var command = await SetupCommandMock(context);

        // Act
        await command.Object.Page(context.PageTarget, "This is a test reason", context.PagingTeamLeader, string.Empty);

        // Assert
        await VerifyPageEmbedEmitted(command, context);
    }

    [Fact(DisplayName = "User should be able to page a team's teamleader")]
    public static async Task PageCommand_Authorized_WhenPagingTeamLeader_ShouldPageCorrectly()
    {
        // Arrange
        var context = new PageCommandTestContext(
            PageTarget.Owner,
            PageTarget.Moderator,
            true
        );

        var command = await SetupCommandMock(context);

        // Act
        await command.Object.Page(context.PageTarget, "This is a test reason", context.PagingTeamLeader, string.Empty);

        // Assert
        await VerifyPageEmbedEmitted(command, context);
    }

    [Fact(DisplayName = "Any staff member should be able to use the Test page")]
    public static async Task PageCommand_AnyStaffMember_WhenPagingTest_ShouldPageCorrectly()
    {
        var targets = Enum.GetValues<PageTarget>().Except([PageTarget.All, PageTarget.Test]);

        foreach (var userTeam in targets)
        {
            // Arrange
            var context = new PageCommandTestContext(
                userTeam,
                PageTarget.Test,
                false
            );

            var command = await SetupCommandMock(context);

            // Act
            await command.Object.Page(context.PageTarget, "This is a test reason", context.PagingTeamLeader,
                string.Empty);

            // Assert
            await VerifyPageEmbedEmitted(command, context);
        }
    }

    [Fact(DisplayName = "Owner should be able to page all")]
    public static async Task PageCommand_Authorized_WhenOwnerPagingAll_ShouldPageCorrectly()
    {
        // Arrange
        var context = new PageCommandTestContext(
            PageTarget.Owner,
            PageTarget.All,
            false
        );

        var command = await SetupCommandMock(context);

        // Act
        await command.Object.Page(context.PageTarget, "This is a test reason", context.PagingTeamLeader, string.Empty);

        // Assert
        await VerifyPageEmbedEmitted(command, context);
    }

    [Fact(DisplayName = "Fail page if paging all teamleader")]
    public static async Task PageCommand_Authorized_WhenOwnerPagingAllTeamLead_ShouldFail()
    {
        // Arrange
        var context = new PageCommandTestContext(
            PageTarget.Owner,
            PageTarget.All,
            true
        );

        var command = await SetupCommandMock(context);

        // Act
        await command.Object.Page(context.PageTarget, "This is a test reason", context.PagingTeamLeader, string.Empty);

        // Assert
        TestUtilities.VerifyMessage(
            command,
            "Failed to send page.  The 'All' team does not have a teamleader.  If intended to page the owner, please select the Owner as the team.",
            true
        );
    }

    [Fact(DisplayName = "Unauthorized user should receive an error message")]
    public static async Task PageCommand_Unauthorized_WhenAttemptingToPage_ShouldFail()
    {
        // Arrange
        var context = new PageCommandTestContext(
            PageTarget.Test,
            PageTarget.Moderator,
            false
        );

        var command = await SetupCommandMock(context);

        // Act
        await command.Object.Page(context.PageTarget, "This is a test reason", context.PagingTeamLeader, string.Empty);

        // Assert
        TestUtilities.VerifyMessage(
            command,
            "You are not authorized to use this command.",
            true
        );
    }

    [Fact(DisplayName = "Helper should not be able to page all")]
    public static async Task PageCommand_Authorized_WhenHelperAttemptsToPageAll_ShouldFail()
    {
        // Arrange
        var context = new PageCommandTestContext(
            PageTarget.Helper,
            PageTarget.All,
            false
        );

        var command = await SetupCommandMock(context);

        // Act
        await command.Object.Page(context.PageTarget, "This is a test reason", context.PagingTeamLeader, string.Empty);

        // Assert
        TestUtilities.VerifyMessage(
            command,
            "You are not authorized to send a page to the entire staff team.",
            true
        );
    } 
    private record PageCommandTestContext(PageTarget UserTeamID, PageTarget PageTarget, bool PagingTeamLeader);
}