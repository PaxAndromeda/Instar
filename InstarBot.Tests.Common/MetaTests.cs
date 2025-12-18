using FluentAssertions;
using Xunit;

namespace InstarBot.Tests;

public static class MetaTests
{
	[Fact]
	public static void MatchesFormat_WithValidText_ShouldReturnTrue()
	{
		const string text = "You are missing an age role.";
		const string format = "You are missing {0} {1} role.";

		bool result = TestUtilities.MatchesFormat(text, format);

		result.Should().BeTrue();
	}
	[Fact]
	public static void MatchesFormat_WithRegexReservedCharacters_ShouldReturnTrue()
	{
		const string text = "You are missing an age **role**.";
		const string format = "You are missing {0} {1} **role**.";

		bool result = TestUtilities.MatchesFormat(text, format);

		result.Should().BeTrue();
	}

	[Theory]
	[InlineData("You are missing  age role.")]
	[InlineData("You are missing an  role.")]
	[InlineData("")]
	[InlineData("luftputefartøyet mitt er fullt av ål")]
	public static void MatchesFormat_WithBadText_ShouldReturnTrue(string text)
	{
		const string format = "You are missing {0} {1} role.";

		bool result = TestUtilities.MatchesFormat(text, format);

		result.Should().BeFalse();
	}
}