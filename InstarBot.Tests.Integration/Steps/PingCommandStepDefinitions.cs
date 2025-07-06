using PaxAndromeda.Instar.Commands;

namespace InstarBot.Tests.Integration;

[Binding]
public class PingCommandStepDefinitions(ScenarioContext context)
{
    [When("the user calls the Ping command")]
    public async Task WhenTheUserCallsThePingCommand()
    {
        var command = TestUtilities.SetupCommandMock<PingCommand>();
        context.Add("Command", command);

        await command.Object.Ping();
    }
}