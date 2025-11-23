using Discord;
using InstarBot.Tests.Services;
using Moq;
using Moq.Protected;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.ConfigModels;
using Xunit;

namespace InstarBot.Tests.Integration.Interactions;

public static class CheckEligibilityCommandTests
{

	private static Mock<CheckEligibilityCommand> SetupCommandMock(CheckEligibilityCommandTestContext context)
	{
		var mockAMS = new Mock<MockAutoMemberSystem>();
		mockAMS.Setup(n => n.CheckEligibility(It.IsAny<InstarDynamicConfiguration>(), It.IsAny<IGuildUser>())).Returns(context.Eligibility);


		List<Snowflake> userRoles = [
			// from Instar.dynamic.test.debug.conf.json: member and new member role, respectively
			context.IsMember ? 793611808372031499ul : 796052052433698817ul
		];

		if (context.IsAMH)
		{
			// from Instar.dynamic.test.debug.conf.json
			userRoles.Add(966434762032054282);
		}

		var commandMock = TestUtilities.SetupCommandMock(
			() => new CheckEligibilityCommand(TestUtilities.GetDynamicConfiguration(), mockAMS.Object, new MockMetricService()),
			new TestContext
			{
				UserRoles = userRoles
			});

		return commandMock;
	}

	private static void VerifyResponse(Mock<CheckEligibilityCommand> command, string expectedString)
	{
		command.Protected().Verify(
				"RespondAsync",
				Times.Once(),
				expectedString,						// text
				ItExpr.IsNull<Embed[]>(),			// embeds
				false,								// isTTS
				true,								// ephemeral
				ItExpr.IsNull<AllowedMentions>(),	// allowedMentions
				ItExpr.IsNull<RequestOptions>(),	// options
				ItExpr.IsNull<MessageComponent>(),	// components
				ItExpr.IsAny<Embed>(),				// embed
				ItExpr.IsAny<PollProperties>(),		// pollProperties
				ItExpr.IsAny<MessageFlags>()		// messageFlags
			);
	}

	private static void VerifyResponseEmbed(Mock<CheckEligibilityCommand> command, CheckEligibilityCommandTestContext ctx)
	{
		// Little more convoluted to verify embed content
		command.Protected().Verify(
				"RespondAsync",
				Times.Once(),
				ItExpr.IsNull<string>(),			// text
				ItExpr.IsNull<Embed[]>(),           // embeds
				false,                              // isTTS
				true,                               // ephemeral
				ItExpr.IsNull<AllowedMentions>(),   // allowedMentions
				ItExpr.IsNull<RequestOptions>(),    // options
				ItExpr.IsNull<MessageComponent>(),  // components
				ItExpr.Is<Embed>(e => e.Description.Contains(ctx.DescriptionPattern) && e.Description.Contains(ctx.DescriptionPattern2) &&
								 e.Fields.Any(n => n.Value.Contains(ctx.MissingItemPattern))
				),                                  // embed
				ItExpr.IsNull<PollProperties>(),    // pollProperties
				ItExpr.IsAny<MessageFlags>()        // messageFlags
			);
	}

	[Fact]
	public static async Task CheckEligibilityCommand_WithExistingMember_ShouldEmitValidMessage()
	{
		// Arrange
		var ctx = new CheckEligibilityCommandTestContext(false, true, MembershipEligibility.Eligible);
		var mock = SetupCommandMock(ctx);

		// Act
		await mock.Object.CheckEligibility();

		// Assert
		VerifyResponse(mock, "You are already a member!");
	}

	[Fact]
	public static async Task CheckEligibilityCommand_WithNewMemberAMH_ShouldEmitValidMessage()
	{
		// Arrange
		var ctx = new CheckEligibilityCommandTestContext(
			true,
			false,
			MembershipEligibility.Eligible,
			"Your membership is currently on hold",
			MissingItemPattern: "The staff will override an administrative hold");

		var mock = SetupCommandMock(ctx);

		// Act
		await mock.Object.CheckEligibility();

		// Assert
		VerifyResponseEmbed(mock, ctx);
	}

	[Theory]
	[InlineData(MembershipEligibility.MissingRoles,        "You are missing an age role.")]
	[InlineData(MembershipEligibility.MissingIntroduction, "You have not posted an introduction in")]
	[InlineData(MembershipEligibility.TooYoung,            "You have not been on the server for")]
	[InlineData(MembershipEligibility.PunishmentReceived,  "You have received a warning or moderator action.")]
	[InlineData(MembershipEligibility.NotEnoughMessages,   "messages in the past")]
	public static async Task CheckEligibilityCommand_WithBadRoles_ShouldEmitValidMessage(MembershipEligibility eligibility, string pattern)
	{
		// Arrange
		string sectionHeader = eligibility switch
		{
			MembershipEligibility.MissingRoles => "Roles",
			MembershipEligibility.MissingIntroduction => "Introduction",
			MembershipEligibility.TooYoung => "Join Age",
			MembershipEligibility.PunishmentReceived => "Mod Actions",
			MembershipEligibility.NotEnoughMessages => "Messages",
			_ => ""
		};

		// Cheeky way to get another section header
		string anotherSectionHeader = (MembershipEligibility) ((int)eligibility << 1) switch
		{
			MembershipEligibility.MissingRoles => "Roles",
			MembershipEligibility.MissingIntroduction => "Introduction",
			MembershipEligibility.TooYoung => "Join Age",
			MembershipEligibility.PunishmentReceived => "Mod Actions",
			MembershipEligibility.NotEnoughMessages => "Messages",
			_ => "Roles"
		};

		var ctx = new CheckEligibilityCommandTestContext(
			false,
			false, 
			MembershipEligibility.NotEligible | eligibility,
			$":x: **{sectionHeader}**",
			$":white_check_mark: **{anotherSectionHeader}",
			pattern);

		var mock = SetupCommandMock(ctx);

		// Act
		await mock.Object.CheckEligibility();

		// Assert
		VerifyResponseEmbed(mock, ctx);
	}

	private record CheckEligibilityCommandTestContext(
		bool IsAMH, 
		bool IsMember, 
		MembershipEligibility Eligibility,
		string DescriptionPattern = "",
		string DescriptionPattern2 = "",
		string MissingItemPattern = "");
}