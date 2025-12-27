using Discord;
using FluentAssertions;
using InstarBot.Test.Framework;
using InstarBot.Test.Framework.Models;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.ConfigModels;
using Xunit;

namespace InstarBot.Tests.Integration.Interactions;

public static class PageCommandTests
{
	private const string TestReason = "Test reason for paging";

    private static async Task<TestOrchestrator> SetupOrchestrator(PageCommandTestContext context)
    {
		var orchestrator = TestOrchestrator.Default;

		if (context.UserTeamID != PageTarget.Test)
			await orchestrator.Actor.AddRoleAsync(GetTeamRole(orchestrator, context.UserTeamID));

		return orchestrator;
    }
    
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
	private static async IAsyncEnumerable<Team> GetTeams(TestOrchestrator orchestrator, PageTarget pageTarget)
    {
		var teamsConfig = orchestrator.Configuration.Teams.ToDictionary(n => n.InternalID, n => n);

		teamsConfig.Should().NotBeNull();

		var teamRefs = pageTarget.GetAttributesOfType<TeamRefAttribute>()?.Select(n => n.InternalID) ?? [];

		foreach (var internalId in teamRefs)
		{
			if (!teamsConfig.TryGetValue(internalId, out var value))
				throw new KeyNotFoundException("Failed to find team with internal ID " + internalId);

			yield return value;
		}
	}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

	private static Snowflake GetTeamRole(TestOrchestrator orchestrator, PageTarget owner)
	{
		var teamId = owner.GetAttributeOfType<TeamRefAttribute>()?.InternalID;

		if (teamId is null)
			throw new InvalidOperationException($"Failed to find a team for {owner}");

		var team = orchestrator.Configuration.Teams.FirstOrDefault(n => n.InternalID.Equals(teamId, StringComparison.Ordinal));

		return team is null ? throw new InvalidOperationException($"Failed to find a team for {owner} (internal ID: {teamId})") : team.ID;
	}

	private static async Task VerifyPageEmbedEmitted(TestOrchestrator orchestrator, Mock<PageCommand> command, PageCommandTestContext context)
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
						await GetTeams(orchestrator, PageTarget.All).Select(n => Snowflake.GetMention(() => n.ID))
							.ToArrayAsync());
					break;
				case PageTarget.Test:
					messageFormat = Strings.Command_Page_TestPageMessage;
					break;
				default:
					var team = await GetTeams(orchestrator, context.PageTarget).FirstAsync();
					messageFormat = Snowflake.GetMention(() => team.ID);
					break;
			}


		command.VerifyResponse(messageFormat, verifier.Build());
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

		var orchestrator = await SetupOrchestrator(context);
		var command = orchestrator.GetCommand<PageCommand>();


        // Act
        await command.Object.Page(context.PageTarget, TestReason, context.PagingTeamLeader, string.Empty);

        // Assert
        await VerifyPageEmbedEmitted(orchestrator, command, context);
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

		var orchestrator = await SetupOrchestrator(context);
		var command = orchestrator.GetCommand<PageCommand>();

		// Act
		await command.Object.Page(context.PageTarget, TestReason, context.PagingTeamLeader, string.Empty);

		// Assert
		await VerifyPageEmbedEmitted(orchestrator, command, context);
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

		var orchestrator = await SetupOrchestrator(context);
		var command = orchestrator.GetCommand<PageCommand>();

		// Act
		await command.Object.Page(context.PageTarget, TestReason, context.PagingTeamLeader, context.Message!, context.TargetUser, context.TargetChannel);

		// Assert
		await VerifyPageEmbedEmitted(orchestrator, command, context);
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

			var orchestrator = await SetupOrchestrator(context);
			var command = orchestrator.GetCommand<PageCommand>();

			// Act
			await command.Object.Page(context.PageTarget, TestReason, context.PagingTeamLeader,
                string.Empty);

            // Assert
            await VerifyPageEmbedEmitted(orchestrator, command, context);
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

		var orchestrator = await SetupOrchestrator(context);
		var command = orchestrator.GetCommand<PageCommand>();

		// Act
		await command.Object.Page(context.PageTarget, TestReason, context.PagingTeamLeader, string.Empty);

        // Assert
        await VerifyPageEmbedEmitted(orchestrator, command, context);
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

		var orchestrator = await SetupOrchestrator(context);
		var command = orchestrator.GetCommand<PageCommand>();

		// Act
		await command.Object.Page(context.PageTarget, TestReason, context.PagingTeamLeader, string.Empty);

		// Assert
		command.VerifyResponse(
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

		var orchestrator = await SetupOrchestrator(context);
		var command = orchestrator.GetCommand<PageCommand>();

		// Act
		await command.Object.Page(context.PageTarget, TestReason, context.PagingTeamLeader, string.Empty);

		// Assert
		command.VerifyResponse(
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

		var orchestrator = await SetupOrchestrator(context);
		var command = orchestrator.GetCommand<PageCommand>();

		// Act
		await command.Object.Page(context.PageTarget, TestReason, context.PagingTeamLeader, string.Empty);

		// Assert
		command.VerifyResponse(
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