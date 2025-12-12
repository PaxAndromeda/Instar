using Discord;
using FluentAssertions;
using InstarBot.Tests.Models;
using InstarBot.Tests.Services;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.DynamoModels;
using Xunit;

namespace InstarBot.Tests.Integration.Interactions;

public static class CheckEligibilityCommandTests
{
	private const ulong MemberRole = 793611808372031499ul;
	private const ulong NewMemberRole = 796052052433698817ul;

	private static async Task<Mock<CheckEligibilityCommand>> SetupCommandMock(CheckEligibilityCommandTestContext context, Action<Mock<MockInstarDDBService>>? setupMocks = null)
	{
		TestUtilities.SetupLogging();

		var mockAMS = new Mock<MockAutoMemberSystem>();
		mockAMS.Setup(n => n.CheckEligibility(It.IsAny<InstarDynamicConfiguration>(), It.IsAny<IGuildUser>())).Returns(context.Eligibility);

		var userId = Snowflake.Generate();

		var mockDDB = new MockInstarDDBService();
		var user = new TestGuildUser
		{
			Id = userId,
			Username = "username",
			JoinedAt = DateTimeOffset.Now,
			RoleIds = context.Roles
		};

		await mockDDB.CreateUserAsync(InstarUserData.CreateFrom(user));

		if (context.IsAMH)
		{
			var ddbUser = await mockDDB.GetUserAsync(userId);

			ddbUser.Should().NotBeNull();
			ddbUser.Data.AutoMemberHoldRecord = new AutoMemberHoldRecord
			{
				Date = DateTime.UtcNow,
				ModeratorID = Snowflake.Generate(),
				Reason = "Testing"
			};

			await ddbUser.UpdateAsync();
		}

		var commandMock = TestUtilities.SetupCommandMock(
			() => new CheckEligibilityCommand(TestUtilities.GetDynamicConfiguration(), mockAMS.Object, mockDDB, new MockMetricService()),
			new TestContext
			{
				UserID = userId,
				UserRoles = context.Roles.Select(n => new Snowflake(n)).ToHashSet()
			});

		context.DDB = mockDDB;
		context.User = user;

		return commandMock;
	}

	private static EmbedVerifier.VerifierBuilder CreateVerifier()
	{
		return EmbedVerifier.Builder()
			.WithTitle(Strings.Command_CheckEligibility_EmbedTitle)
			.WithFooterText(Strings.Embed_AMS_Footer);
	}


	[Fact]
	public static async Task CheckEligibilityCommand_WithExistingMember_ShouldEmitValidMessage()
	{
		// Arrange
		var ctx = new CheckEligibilityCommandTestContext(false, [ MemberRole ], MembershipEligibility.Eligible);
		var mock = await SetupCommandMock(ctx);

		// Act
		await mock.Object.CheckEligibility();

		// Assert
		TestUtilities.VerifyMessage(mock, Strings.Command_CheckEligibility_Error_AlreadyMember, true);
	}

	[Fact]
	public static async Task CheckEligibilityCommand_NoMemberRoles_ShouldEmitValidErrorMessage()
	{
		// Arrange
		var ctx = new CheckEligibilityCommandTestContext(false, [], MembershipEligibility.Eligible);
		var mock = await SetupCommandMock(ctx);

		// Act
		await mock.Object.CheckEligibility();

		// Assert
		TestUtilities.VerifyMessage(mock, Strings.Command_CheckEligibility_Error_NoMemberRoles, true);
	}

	[Fact]
	public static async Task CheckEligibilityCommand_Eligible_ShouldEmitValidMessage()
	{
		// Arrange
		var verifier = CreateVerifier()
			.WithFlags(EmbedVerifierMatchFlags.PartialDescription)
			.WithDescription(Strings.Command_CheckEligibility_MessagesEligibility)
			.Build();

		var ctx = new CheckEligibilityCommandTestContext(
			false,
			[ NewMemberRole ],
			MembershipEligibility.Eligible);


		var mock = await SetupCommandMock(ctx);

		// Act
		await mock.Object.CheckEligibility();

		// Assert
		TestUtilities.VerifyEmbed(mock, verifier, ephemeral: true);
	}

	[Fact]
	public static async Task CheckEligibilityCommand_WithNewMemberAMH_ShouldEmitValidMessage()
	{
		// Arrange
		var verifier = CreateVerifier()
			.WithFlags(EmbedVerifierMatchFlags.PartialDescription)
			.WithDescription(Strings.Command_CheckEligibility_AMH_MembershipWithheld)
			.WithField(Strings.Command_CheckEligibility_AMH_Why)
			.WithField(Strings.Command_CheckEligibility_AMH_WhatToDo)
			.WithField(Strings.Command_CheckEligibility_AMH_ContactStaff)
			.Build();

		var ctx = new CheckEligibilityCommandTestContext(
			true,
			[ NewMemberRole ],
			MembershipEligibility.Eligible);


		var mock = await SetupCommandMock(ctx);

		// Act
		await mock.Object.CheckEligibility();

		// Assert
		TestUtilities.VerifyEmbed(mock, verifier, ephemeral: true);
	}

	[Fact]
	public static async Task CheckEligibilityCommand_DDBError_ShouldEmitValidMessage()
	{
		// Arrange
		var verifier = CreateVerifier()
			.WithFlags(EmbedVerifierMatchFlags.PartialDescription)
			.WithDescription(Strings.Command_CheckEligibility_MessagesEligibility)
			.Build();

		var ctx = new CheckEligibilityCommandTestContext(
			true,
			[ NewMemberRole ],
			MembershipEligibility.Eligible);

		var mock = await SetupCommandMock(ctx);

		ctx.DDB.Setup(n => n.GetUserAsync(It.IsAny<Snowflake>())).Throws<BadStateException>();

		// Act
		await mock.Object.CheckEligibility();

		// Assert
		TestUtilities.VerifyEmbed(mock, verifier, ephemeral: true);
	}

	[Theory]
	[InlineData(MembershipEligibility.MissingRoles)]
	[InlineData(MembershipEligibility.MissingIntroduction)]
	[InlineData(MembershipEligibility.InadequateTenure)]
	[InlineData(MembershipEligibility.PunishmentReceived)]
	[InlineData(MembershipEligibility.NotEnoughMessages)]
	public static async Task CheckEligibilityCommand_WithBadRoles_ShouldEmitValidMessage(MembershipEligibility eligibility)
	{
		// Arrange
		var eligibilityMap = new Dictionary<MembershipEligibility, string>
		{
			{ MembershipEligibility.MissingRoles,        Strings.Command_CheckEligibility_MessagesEligibility },
			{ MembershipEligibility.MissingIntroduction, Strings.Command_CheckEligibility_IntroductionEligibility },
			{ MembershipEligibility.InadequateTenure,            Strings.Command_CheckEligibility_JoinAgeEligibility },
			{ MembershipEligibility.PunishmentReceived,  Strings.Command_CheckEligibility_ModActionsEligibility },
			{ MembershipEligibility.NotEnoughMessages,   Strings.Command_CheckEligibility_MessagesEligibility },
		};

		var fieldMap = new Dictionary<MembershipEligibility, string>
		{
			{ MembershipEligibility.MissingRoles,        Strings.Command_CheckEligibility_MissingItem_Role },
			{ MembershipEligibility.MissingIntroduction, Strings.Command_CheckEligibility_MissingItem_Introduction },
			{ MembershipEligibility.InadequateTenure,            Strings.Command_CheckEligibility_MissingItem_TooYoung },
			{ MembershipEligibility.PunishmentReceived,  Strings.Command_CheckEligibility_MissingItem_PunishmentReceived },
			{ MembershipEligibility.NotEnoughMessages,   Strings.Command_CheckEligibility_MissingItem_Messages },
		};

		var testDescription = eligibilityMap.GetValueOrDefault(eligibility, string.Empty);
		//var nontestFieldFormat = eligibilityMap.GetValueOrDefault(eligibility, eligibilityMap[MembershipEligibility.MissingRoles]);
		var testFieldMap = fieldMap.GetValueOrDefault(eligibility, string.Empty);

		var verifier = CreateVerifier()
			.WithFlags(EmbedVerifierMatchFlags.PartialDescription)
			.WithDescription(testDescription)
			.WithField(testFieldMap, true)
			.Build();

		var ctx = new CheckEligibilityCommandTestContext(
			false,
			[ NewMemberRole ], 
			eligibility);

		var mock = await SetupCommandMock(ctx);

		// Act
		await mock.Object.CheckEligibility();

		// Assert
		TestUtilities.VerifyEmbed(mock, verifier, ephemeral: true);
	}

	[Fact]
	public static async Task CheckOtherEligibility_WithEligibleMember_ShouldEmitValidEmbed()
	{
		// Arrange
		var ctx = new CheckEligibilityCommandTestContext(false, [ NewMemberRole ], MembershipEligibility.Eligible);
		var mock = await SetupCommandMock(ctx);

		ctx.User.Should().NotBeNull();
		var verifier = CreateVerifier()
			.WithDescription(Strings.Command_Eligibility_EligibleText)
			.WithAuthorName(ctx.User.Username)
			.Build();

		// Act
		await mock.Object.CheckOtherEligibility(ctx.User);

		// Assert
		TestUtilities.VerifyEmbed(mock, verifier, ephemeral: true);
	}

	[Fact]
	public static async Task CheckOtherEligibility_WithIneligibleMember_ShouldEmitValidEmbed()
	{
		// Arrange
		var ctx = new CheckEligibilityCommandTestContext(false, [ NewMemberRole ], MembershipEligibility.MissingRoles);
		var mock = await SetupCommandMock(ctx);

		ctx.User.Should().NotBeNull();
		var verifier = CreateVerifier()
			.WithDescription(Strings.Command_Eligibility_IneligibleText)
			.WithAuthorName(ctx.User.Username)
			.WithField(Strings.Command_Eligibility_Section_Requirements, Strings.Command_CheckEligibility_RolesEligibility, true)
			.Build();

		// Act
		await mock.Object.CheckOtherEligibility(ctx.User);

		// Assert
		TestUtilities.VerifyEmbed(mock, verifier, ephemeral: true);
	}

	[Fact]
	public static async Task CheckOtherEligibility_WithAMHedMember_ShouldEmitValidEmbed()
	{
		// Arrange
		var ctx = new CheckEligibilityCommandTestContext(true, [ NewMemberRole ],  MembershipEligibility.MissingRoles);
		var mock = await SetupCommandMock(ctx);

		ctx.User.Should().NotBeNull();
		var verifier = CreateVerifier()
			.WithDescription(Strings.Command_Eligibility_IneligibleText)
			.WithAuthorName(ctx.User.Username)
			.WithField(Strings.Command_Eligibility_Section_Hold, Strings.Command_Eligibility_HoldFormat)
			.Build();

		// Act
		await mock.Object.CheckOtherEligibility(ctx.User);

		// Assert
		TestUtilities.VerifyEmbed(mock, verifier, ephemeral: true);
	}

	[Fact]
	public static async Task CheckOtherEligibility_WithDynamoError_ShouldEmitValidEmbed()
	{
		// Arrange
		var ctx = new CheckEligibilityCommandTestContext(false, [ NewMemberRole ], MembershipEligibility.MissingRoles);
		var mock = await SetupCommandMock(ctx);

		ctx.DDB.Setup(n => n.GetUserAsync(It.IsAny<Snowflake>()))
			.Throws(new BadStateException("Bad state"));


		ctx.User.Should().NotBeNull();
		var verifier = CreateVerifier()
			.WithDescription(Strings.Command_Eligibility_IneligibleText)
			.WithAuthorName(ctx.User.Username)
			.WithField(Strings.Command_Eligibility_Section_Requirements, Strings.Command_CheckEligibility_RolesEligibility, true)
			.WithField(Strings.Command_Eligibility_Section_AmbiguousHold, Strings.Command_Eligibility_Error_AmbiguousHold)
			.Build();

		// Act
		await mock.Object.CheckOtherEligibility(ctx.User);

		// Assert
		TestUtilities.VerifyEmbed(mock, verifier, ephemeral: true);
	}

	private record CheckEligibilityCommandTestContext(
		bool IsAMH, 
		List<ulong> Roles, 
		MembershipEligibility Eligibility)
	{
		internal IGuildUser? User { get; set; }
		public MockInstarDDBService DDB { get ; set ; }
	}
}