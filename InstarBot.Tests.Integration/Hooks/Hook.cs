using Xunit;

namespace InstarBot.Tests.Integration.Hooks;

[Binding]
public class InstarHooks(ScenarioContext scenarioContext)
{
    [Then(@"Instar should emit a message stating ""(.*)""")]
    public void ThenInstarShouldEmitAMessageStating(string message)
    {
        Assert.True(scenarioContext.ContainsKey("Command"));
        var cmdObject = scenarioContext.Get<object>("Command");
        TestUtilities.VerifyMessage(cmdObject, message);
    }

    [Then(@"Instar should emit an ephemeral message stating ""(.*)""")]
    public void ThenInstarShouldEmitAnEphemeralMessageStating(string message)
    {
        Assert.True(scenarioContext.ContainsKey("Command"));
        var cmdObject = scenarioContext.Get<object>("Command");
        TestUtilities.VerifyMessage(cmdObject, message, true);
    }
}