using Discord;
using FluentAssertions;
using InstarBot.Test.Framework;
using InstarBot.Test.Framework.Services;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Services;
using Xunit;

namespace InstarBot.Tests.Integration.Interactions;

public static class CheckEligibilityCommandTests
{
	private static TestOrchestrator SetupOrchestrator(MembershipEligibility eligibility)
	{
		var orchestrator = TestOrchestrator.Default;

		TestAutoMemberSystem tams = (TestAutoMemberSystem) orchestrator.GetService<IAutoMemberSystem>();
		tams.Mock.Setup(n => n.CheckEligibility(It.IsAny<InstarDynamicConfiguration>(), It.IsAny<IGuildUser>())).Returns(eligibility);

		return orchestrator;
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
		var orchestrator = SetupOrchestrator(MembershipEligibility.Eligible);
		var cmd = orchestrator.GetCommand<CheckEligibilityCommand>();

		await orchestrator.Actor.AddRoleAsync(orchestrator.Configuration.MemberRoleID);

		// Act
		await cmd.Object.CheckEligibility();

		// Assert
		cmd.VerifyResponse(Strings.Command_CheckEligibility_Error_AlreadyMember, ephemeral: true);
	}

	[Fact]
	public static async Task CheckEligibilityCommand_NoMemberRoles_ShouldEmitValidErrorMessage()
	{
		// Arrange
		var orchestrator = SetupOrchestrator(MembershipEligibility.Eligible);
		var cmd = orchestrator.GetCommand<CheckEligibilityCommand>();

		// Act
		await cmd.Object.CheckEligibility();

		// Assert
		cmd.VerifyResponse(Strings.Command_CheckEligibility_Error_NoMemberRoles, ephemeral: true);
	}

	[Fact]
	public static async Task CheckEligibilityCommand_Eligible_ShouldEmitValidMessage()
	{
		// Arrange
		var verifier = CreateVerifier()
			.WithFlags(EmbedVerifierMatchFlags.PartialDescription)
			.WithDescription(Strings.Command_CheckEligibility_MessagesEligibility)
			.Build();

		var orchestrator = SetupOrchestrator(MembershipEligibility.Eligible);
		var cmd = orchestrator.GetCommand<CheckEligibilityCommand>();

		await orchestrator.Actor.AddRoleAsync(orchestrator.Configuration.NewMemberRoleID);

		// Act
		await cmd.Object.CheckEligibility();

		// Assert
		cmd.VerifyResponse(verifier, ephemeral: true);
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
		
		var orchestrator = SetupOrchestrator(MembershipEligibility.Eligible);
		var cmd = orchestrator.GetCommand<CheckEligibilityCommand>();

		await orchestrator.Actor.AddRoleAsync(orchestrator.Configuration.NewMemberRoleID);

		// Give an AMH
		var dbUser = await orchestrator.Database.GetUserAsync(orchestrator.Actor.Id);
		dbUser.Should().NotBeNull();
		dbUser.Data.AutoMemberHoldRecord = new AutoMemberHoldRecord
		{
			Date = DateTime.UtcNow,
			ModeratorID = Snowflake.Generate(),
			Reason = "Testing"
		};

		await dbUser.CommitAsync();

		// Act
		await cmd.Object.CheckEligibility();

		// Assert
		cmd.VerifyResponse(verifier, ephemeral: true);
	}

	[Fact]
	public static async Task CheckEligibilityCommand_DDBError_ShouldEmitValidMessage()
	{
		// Arrange
		var verifier = CreateVerifier()
			.WithFlags(EmbedVerifierMatchFlags.PartialDescription)
			.WithDescription(Strings.Command_CheckEligibility_MessagesEligibility)
			.Build();

		var orchestrator = SetupOrchestrator(MembershipEligibility.Eligible);
		var cmd = orchestrator.GetCommand<CheckEligibilityCommand>();

		await orchestrator.Actor.AddRoleAsync(orchestrator.Configuration.NewMemberRoleID);

		if (orchestrator.Database is not IMockOf<IDatabaseService> dbMock)
			throw new InvalidOperationException("This test depends on the registered database implementing IMockOf<IDatabaseService>");

		dbMock.Mock.Setup(n => n.GetUserAsync(It.IsAny<Snowflake>())).Throws<BadStateException>();

		// Act
		await cmd.Object.CheckEligibility();

		// Assert
		cmd.VerifyResponse(verifier, ephemeral: true);
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
			{ MembershipEligibility.InadequateTenure,    Strings.Command_CheckEligibility_JoinAgeEligibility },
			{ MembershipEligibility.PunishmentReceived,  Strings.Command_CheckEligibility_ModActionsEligibility },
			{ MembershipEligibility.NotEnoughMessages,   Strings.Command_CheckEligibility_MessagesEligibility },
		};

		var fieldMap = new Dictionary<MembershipEligibility, string>
		{
			{ MembershipEligibility.MissingRoles,        Strings.Command_CheckEligibility_MissingItem_Role },
			{ MembershipEligibility.MissingIntroduction, Strings.Command_CheckEligibility_MissingItem_Introduction },
			{ MembershipEligibility.InadequateTenure,    Strings.Command_CheckEligibility_MissingItem_TooYoung },
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

		var orchestrator = SetupOrchestrator(eligibility);
		var cmd = orchestrator.GetCommand<CheckEligibilityCommand>();

		await orchestrator.Actor.AddRoleAsync(orchestrator.Configuration.NewMemberRoleID);

		// Act
		await cmd.Object.CheckEligibility();

		// Assert
		cmd.VerifyResponse(verifier, ephemeral: true);
	}

	[Fact]
	public static async Task CheckOtherEligibility_WithEligibleMember_ShouldEmitValidEmbed()
	{
		// Arrange

		var orchestrator = SetupOrchestrator(MembershipEligibility.Eligible);
		var cmd = orchestrator.GetCommand<CheckEligibilityCommand>();

		await orchestrator.Subject.AddRoleAsync(orchestrator.Configuration.NewMemberRoleID);

		var verifier = CreateVerifier()
			.WithDescription(Strings.Command_Eligibility_EligibleText)
			.WithAuthorName(orchestrator.Subject.Username)
			.Build();

		// Act
		await cmd.Object.CheckOtherEligibility(orchestrator.Subject);

		// Assert
		cmd.VerifyResponse(verifier, ephemeral: true);
	}

	[Fact]
	public static async Task CheckOtherEligibility_WithIneligibleMember_ShouldEmitValidEmbed()
	{
		// Arrange
		var orchestrator = SetupOrchestrator(MembershipEligibility.MissingRoles);
		var cmd = orchestrator.GetCommand<CheckEligibilityCommand>();

		await orchestrator.Subject.AddRoleAsync(orchestrator.Configuration.NewMemberRoleID);
		var verifier = CreateVerifier()
			.WithDescription(Strings.Command_Eligibility_IneligibleText)
			.WithAuthorName(orchestrator.Subject.Username)
			.WithField(Strings.Command_Eligibility_Section_Requirements, Strings.Command_CheckEligibility_RolesEligibility, true)
			.Build();

		// Act
		await cmd.Object.CheckOtherEligibility(orchestrator.Subject);

		// Assert
		cmd.VerifyResponse(verifier, ephemeral: true);
	}

	[Fact]
	public static async Task CheckOtherEligibility_WithAMHedMember_ShouldEmitValidEmbed()
	{
		// Arrange
		var orchestrator = SetupOrchestrator(MembershipEligibility.MissingRoles);
		var cmd = orchestrator.GetCommand<CheckEligibilityCommand>();

		var verifier = CreateVerifier()
			.WithDescription(Strings.Command_Eligibility_IneligibleText)
			.WithAuthorName(orchestrator.Subject.Username)
			.WithField(Strings.Command_Eligibility_Section_Hold, Strings.Command_Eligibility_HoldFormat)
			.Build();

		await orchestrator.Subject.AddRoleAsync(orchestrator.Configuration.NewMemberRoleID);

		// Give the subject an AMH
		var dbUser = await orchestrator.Database.GetUserAsync(orchestrator.Subject.Id);
		dbUser.Should().NotBeNull();
		dbUser.Data.AutoMemberHoldRecord = new AutoMemberHoldRecord
		{
			Date = DateTime.UtcNow,
			ModeratorID = Snowflake.Generate(),
			Reason = "Testing"
		};

		await dbUser.CommitAsync();

		// Act
		await cmd.Object.CheckOtherEligibility(orchestrator.Subject);

		// Assert
		cmd.VerifyResponse(verifier, ephemeral: true);
	}

	[Fact]
	public static async Task CheckOtherEligibility_WithDynamoError_ShouldEmitValidEmbed()
	{
		// Arrange
		var orchestrator = SetupOrchestrator(MembershipEligibility.MissingRoles);
		var cmd = orchestrator.GetCommand<CheckEligibilityCommand>();

		if (orchestrator.Database is not IMockOf<IDatabaseService> dbMock)
			throw new InvalidOperationException("This test depends on the registered database implementing IMockOf<IDatabaseService>");

		dbMock.Mock.Setup(n => n.GetOrCreateUserAsync(It.Is<IGuildUser>(guildUser => guildUser.Id == orchestrator.Subject.Id))).Throws<BadStateException>();

		var verifier = CreateVerifier()
			.WithDescription(Strings.Command_Eligibility_IneligibleText)
			.WithAuthorName(orchestrator.Subject.Username)
			.WithField(Strings.Command_Eligibility_Section_Requirements, Strings.Command_CheckEligibility_RolesEligibility, true)
			.WithField(Strings.Command_Eligibility_Section_AmbiguousHold, Strings.Command_Eligibility_Error_AmbiguousHold)
			.Build();

		// Act
		await cmd.Object.CheckOtherEligibility(orchestrator.Subject);

		// Assert
		cmd.VerifyResponse(verifier, ephemeral: true);
	}
}