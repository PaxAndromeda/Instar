using Discord;
using InstarBot.Tests.Models;
using InstarBot.Tests.Services;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using Xunit;

namespace InstarBot.Tests.Integration.Interactions;

public static class PageCommandTests
{
	private const string TestReason = "Test reason for paging";

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
		var verifier = EmbedVerifier.Builder()
			.WithFooterText(Strings.Embed_Page_Footer)
			.WithDescription("```{0}```");

		if (context.TargetUser is not null)
			verifier = verifier.WithField(
				"User",
				$"<@{context.TargetUser.Id}>");
		
		if (context.TargetChannel is not null)
			verifier = verifier.WithField(
				"Channel",
				$"<#{context.TargetChannel.Id}>");

		if (context.Message is not null)
			verifier = verifier.WithField(
				"Message",
				$"{context.Message}");

		string messageFormat;
		if (context.PagingTeamLeader)
			messageFormat = "<@{0}>";
		else
			switch (context.PageTarget)
			{
				case PageTarget.All:
					messageFormat = string.Join(' ',
						await TestUtilities.GetTeams(PageTarget.All).Select(n => Snowflake.GetMention(() => n.ID))
							.ToArrayAsync());
					break;
				case PageTarget.Test:
					messageFormat = Strings.Command_Page_TestPageMessage;
					break;
				default:
					var team = await TestUtilities.GetTeams(context.PageTarget).FirstAsync();
					messageFormat = Snowflake.GetMention(() => team.ID);
					break;
			}


		TestUtilities.VerifyEmbed(command, verifier.Build(), messageFormat);
    }

    [Fact(DisplayName = "User should be able to page when authorized")]
    public static async Task PageCommand_Authorized_WhenPagingTeam_ShouldPageCorrectly()
    {
        // Arrange
		TestUtilities.SetupLogging();
        var context = new PageCommandTestContext(
            PageTarget.Owner,
            PageTarget.Moderator,
            false
        );

        var command = await SetupCommandMock(context);

        // Act
        await command.Object.Page(context.PageTarget, TestReason, context.PagingTeamLeader, string.Empty);

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
		await command.Object.Page(context.PageTarget, TestReason, context.PagingTeamLeader, string.Empty);

		// Assert
		await VerifyPageEmbedEmitted(command, context);
	}

	[Fact(DisplayName = "User should be able to page a with user, channel and message")]
	public static async Task PageCommand_Authorized_WhenPagingWithData_ShouldPageCorrectly()
	{
		// Arrange
		var context = new PageCommandTestContext(
			PageTarget.Owner,
			PageTarget.Moderator,
			true,
			TargetUser: new TestUser(Snowflake.Generate()),
			TargetChannel: new TestChannel(Snowflake.Generate()),
			Message: "<message link>"
		);

		var command = await SetupCommandMock(context);

		// Act
		await command.Object.Page(context.PageTarget, TestReason, context.PagingTeamLeader, context.Message!, context.TargetUser, context.TargetChannel);

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
            await command.Object.Page(context.PageTarget, TestReason, context.PagingTeamLeader,
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
        await command.Object.Page(context.PageTarget, TestReason, context.PagingTeamLeader, string.Empty);

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
        await command.Object.Page(context.PageTarget, TestReason, context.PagingTeamLeader, string.Empty);

        // Assert
        TestUtilities.VerifyMessage(
            command,
            Strings.Command_Page_Error_NoAllTeamlead,
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
        await command.Object.Page(context.PageTarget, TestReason, context.PagingTeamLeader, string.Empty);

        // Assert
        TestUtilities.VerifyMessage(
            command,
			Strings.Command_Page_Error_NotAuthorized,
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
        await command.Object.Page(context.PageTarget, TestReason, context.PagingTeamLeader, string.Empty);

        // Assert
        TestUtilities.VerifyMessage(
            command,
			Strings.Command_Page_Error_FullTeamNotAuthorized,
            true
        );
    } 
    private record PageCommandTestContext(
		PageTarget UserTeamID, 
		PageTarget PageTarget, 
		bool PagingTeamLeader,
		IUser? TargetUser = null,
		IChannel? TargetChannel = null,
		string? Message = null);
}