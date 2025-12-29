using System;
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

	[Theory]
	[InlineData(1, "st")]
	[InlineData(2, "nd")]
	[InlineData(3, "rd")]
	[InlineData(4, "th")]
	[InlineData(1011, "th")]
	public void GetOrdinal_ShouldProduceValidOutput(int input, string expected)
	{
		Utilities.GetOrdinal(input).Should().Be(expected);
	}

	[Theory]
	[InlineData(1, "st")]
	[InlineData(2, "nd")]
	[InlineData(3, "rd")]
	[InlineData(4, "th")]
	public void DateTimeOffsetExtension_ToString_WithExtended_ShouldProduceValidOutput(int day, string expectedSuffix)
	{
		// Arrange
		var dateTime = new DateTimeOffset(new DateTime(2020, 3, day));

		// Act
		var result = dateTime.ToString("MMMM dnn", true);

		// Assert
		result.Should().Be($"March {day}{expectedSuffix}");
	}
}