using FluentAssertions;
using PaxAndromeda.Instar;
using Xunit;

namespace InstarBot.Tests;

public class UtilitiesTests
{
    [Theory]
    [InlineData("OWNER", "Owner")]
    [InlineData("ADMIN", "Admin")]
    [InlineData("MODERATOR", "Moderator")]
    [InlineData("SENIOR_HELPER", "SeniorHelper")]
    [InlineData("HELPER", "Helper")]
    [InlineData("COMMUNITY_MANAGER", "CommunityManager")]
    [InlineData("MEMBER", "Member")]
    [InlineData("NEW_MEMBER", "NewMember")]
    public void ScreamingToSnakeCase_ShouldProduceValidSnakeCase(string input, string expected)
    {
        Utilities.ScreamingToPascalCase(input).Should().Be(expected);
    }
}