using System.Diagnostics.CodeAnalysis;
using Discord;
using FluentAssertions;
using InstarBot.Tests.Services;
using Moq;
using Moq.Protected;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;

namespace InstarBot.Tests.Integration;

[Binding]
public sealed class PageCommandStepDefinitions(ScenarioContext scenarioContext)
{
    [Given("the user is in team (.*)")]
    public async Task GivenTheUserIsInTeam(PageTarget target)
    {
        var team = await TestUtilities.GetTeams(target).FirstAsync();
        scenarioContext.Add("UserTeamID", team.ID);
    }

    [Given("the user is not a staff member")]
    public void GivenTheUserIsNotAStaffMember()
    {
        scenarioContext.Add("UserTeamID", new Snowflake());
    }

    [Given("the user is paging (Helper|Moderator|Admin|Owner|Test|All)")]
    [SuppressMessage("ReSharper", "SpecFlow.MethodNameMismatchPattern")]
    public void GivenTheUserIsPaging(PageTarget target)
    {
        scenarioContext.Add("PageTarget", target);
        scenarioContext.Add("PagingTeamLeader", false);
    }

    [Given("the user is paging the (Helper|Moderator|Admin|Owner|Test|All) teamleader")]
    [SuppressMessage("ReSharper", "SpecFlow.MethodNameMismatchPattern")]
    public void GivenTheUserIsPagingTheTeamTeamleader(PageTarget target)
    {
        scenarioContext.Add("PageTarget", target);
        scenarioContext.Add("PagingTeamLeader", true);
    }

    [When("the user calls the Page command")]
    public async Task WhenTheUserCallsThePageCommand()
    {
        scenarioContext.ContainsKey("PageTarget").Should().BeTrue();
        scenarioContext.ContainsKey("PagingTeamLeader").Should().BeTrue();
        var pageTarget = scenarioContext.Get<PageTarget>("PageTarget");
        var pagingTeamLeader = scenarioContext.Get<bool>("PagingTeamLeader");

        var command = SetupMocks();
        scenarioContext.Add("Command", command);

        await command.Object.Page(pageTarget, "This is a test reason", pagingTeamLeader);
    }

    [Then("Instar should emit a valid Page embed")]
    public async Task ThenInstarShouldEmitAValidPageEmbed()
    {
        scenarioContext.ContainsKey("Command").Should().BeTrue();
        scenarioContext.ContainsKey("PageTarget").Should().BeTrue();
        var command = scenarioContext.Get<Mock<PageCommand>>("Command");
        var pageTarget = scenarioContext.Get<PageTarget>("PageTarget");

        string expectedString;

        if (pageTarget == PageTarget.Test)
        {
            expectedString = "This is a __**TEST**__ page.";
        }
        else
        {
            var team = await TestUtilities.GetTeams(pageTarget).FirstAsync();
            expectedString = Snowflake.GetMention(() => team.ID);
        }

        command.Protected().Verify(
            "RespondAsync", Times.Once(),
            expectedString, ItExpr.IsNull<Embed[]>(),
            false, false, AllowedMentions.All, ItExpr.IsNull<RequestOptions>(),
            ItExpr.IsNull<MessageComponent>(), ItExpr.IsAny<Embed>(), ItExpr.IsAny<PollProperties>());
    }

    [Then("Instar should emit a valid teamleader Page embed")]
    public async Task ThenInstarShouldEmitAValidTeamleaderPageEmbed()
    {
        scenarioContext.ContainsKey("Command").Should().BeTrue();
        scenarioContext.ContainsKey("PageTarget").Should().BeTrue();

        var command = scenarioContext.Get<Mock<PageCommand>>("Command");
        var pageTarget = scenarioContext.Get<PageTarget>("PageTarget");

        command.Protected().Verify(
            "RespondAsync", Times.Once(),
            $"<@{await GetTeamLead(pageTarget)}>", ItExpr.IsNull<Embed[]>(),
            false, false, AllowedMentions.All, ItExpr.IsNull<RequestOptions>(),
            ItExpr.IsNull<MessageComponent>(), ItExpr.IsAny<Embed>(), ItExpr.IsAny<PollProperties>());
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

    [Then("Instar should emit a valid All Page embed")]
    public async Task ThenInstarShouldEmitAValidAllPageEmbed()
    {
        scenarioContext.ContainsKey("Command").Should().BeTrue();
        var command = scenarioContext.Get<Mock<PageCommand>>("Command");
        var expected = string.Join(' ',
            await TestUtilities.GetTeams(PageTarget.All).Select(n => Snowflake.GetMention(() => n.ID)).ToArrayAsync());

        command.Protected().Verify(
            "RespondAsync", Times.Once(),
            expected, ItExpr.IsNull<Embed[]>(),
            false, false, AllowedMentions.All, ItExpr.IsNull<RequestOptions>(),
            ItExpr.IsNull<MessageComponent>(), ItExpr.IsAny<Embed>(), ItExpr.IsAny<PollProperties>());
    }

    private Mock<PageCommand> SetupMocks()
    {
        var userTeam = scenarioContext.Get<Snowflake>("UserTeamID");

        var commandMock = TestUtilities.SetupCommandMock(
            () => new PageCommand(TestUtilities.GetTeamService(), new MockMetricService()),
            new TestContext
            {
                UserRoles = [userTeam]
            });

        return commandMock;
    }
}