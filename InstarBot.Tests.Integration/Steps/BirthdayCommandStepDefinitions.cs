using FluentAssertions;
using InstarBot.Tests.Services;
using Microsoft.Extensions.DependencyInjection;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.Services;
using TechTalk.SpecFlow.Assist;
using Xunit;

namespace InstarBot.Tests.Integration;

[Binding]
public class BirthdayCommandStepDefinitions(ScenarioContext context)
{
    [Given("the user provides the following parameters")]
    public void GivenTheUserProvidesTheFollowingParameters(Table table)
    {
        var dict = table.Rows.ToDictionary(n => n["Key"], n => n.GetInt32("Value"));

        // Let's see if we have the bare minimum
        Assert.True(dict.ContainsKey("Year") && dict.ContainsKey("Month") && dict.ContainsKey("Day"));

        context.Add("Year", dict["Year"]);
        context.Add("Month", dict["Month"]);
        context.Add("Day", dict["Day"]);

        if (dict.TryGetValue("Timezone", out var value))
            context.Add("Timezone", value);
    }

    [When("the user calls the Set Birthday command")]
    public async Task WhenTheUserCallsTheSetBirthdayCommand()
    {
        var year = context.Get<int>("Year");
        var month = context.Get<int>("Month");
        var day = context.Get<int>("Day");
        var timezone = context.ContainsKey("Timezone") ? context.Get<int>("Timezone") : 0;

        var userId = new Snowflake().ID;

        var ddbService = TestUtilities.GetServices().GetService<IInstarDDBService>();
        var cmd = TestUtilities.SetupCommandMock(() => new SetBirthdayCommand(ddbService!, new MockMetricService()), new TestContext
        {
            UserID = userId
        });
        context.Add("Command", cmd);
        context.Add("UserID", userId);
        context.Add("DDB", ddbService);

        await cmd.Object.SetBirthday((Month)month, day, year, timezone);
    }

    [Then("DynamoDB should have the user's (Birthday|JoinDate) set to (.*)")]
    public async Task ThenDynamoDbShouldHaveBirthdaySetTo(string dataType, DateTime time)
    {
        var ddbService = context.Get<IInstarDDBService>("DDB");
        var userId = context.Get<ulong>("UserID");

        switch (dataType)
        {
            case "Birthday":
                (await ddbService!.GetUserBirthday(userId)).Should().Be(time.ToUniversalTime());
                break;
            case "JoinDate":
                (await ddbService!.GetUserJoinDate(userId)).Should().Be(time.ToUniversalTime());
                break;
            default:
                Assert.Fail("Invalid test setup: dataType is unknown");
                break;
        }
    }
}