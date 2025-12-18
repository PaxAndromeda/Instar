using Discord;
using FluentAssertions;
using InstarBot.Test.Framework;
using InstarBot.Test.Framework.Models;
using InstarBot.Test.Framework.Services;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Metrics;
using PaxAndromeda.Instar.Services;
using Xunit;

namespace InstarBot.Tests.Integration.Interactions;

public static class AutoMemberSystemCommandTests
{
	private const ulong NewMemberRole = 796052052433698817ul;
	private const ulong MemberRole = 793611808372031499ul;

	/*
	private static async Task<Context> Setup(bool setupAMH = false)
	{
		TestUtilities.SetupLogging();

		// This is going to be annoying
		var userID = Snowflake.Generate();
		var modID = Snowflake.Generate();

		var mockDDB = new MockDatabaseService();
		var mockMetrics = new MockMetricService();

		

		var user = new TestGuildUser
		{
			Id = userID,
			Username = "username",
			JoinedAt = DateTimeOffset.UtcNow,
			RoleIds = [ NewMemberRole ]
		};

		var mod = new TestGuildUser
		{
			Id = modID,
			Username = "mod_username",
			JoinedAt = DateTimeOffset.UtcNow,
			RoleIds = [MemberRole]
		};

		await mockDDB.CreateUserAsync(InstarUserData.CreateFrom(user));
		await mockDDB.CreateUserAsync(InstarUserData.CreateFrom(mod));

		if (setupAMH)
		{
			var ddbRecord = await mockDDB.GetUserAsync(userID);
			ddbRecord.Should().NotBeNull();
			ddbRecord.Data.AutoMemberHoldRecord = new AutoMemberHoldRecord
			{
				Date = DateTime.UtcNow,
				ModeratorID = modID,
				Reason = "test reason"
			};
			await ddbRecord.CommitAsync();
		}

		var commandMock = TestUtilities.SetupCommandMock(
			() => new AutoMemberHoldCommand(mockDDB, TestUtilities.GetDynamicConfiguration(), mockMetrics, TimeProvider.System),
			new TestContext
			{
				UserID = modID
			});

		return new Context(mockDDB, mockMetrics, user, mod, commandMock);
	}
	*/

	[Fact]
	public static async Task HoldMember_WithValidUserAndReason_ShouldCreateRecord()
	{
		// Arrange
		var orchestrator = TestOrchestrator.Default;
		var cmd = orchestrator.GetCommand<AutoMemberHoldCommand>();

		// Act
		await cmd.Object.HoldMember(orchestrator.Subject, "Test reason");

		// Assert
		cmd.VerifyResponse(Strings.Command_AutoMemberHold_Success, ephemeral: true);

		var record = await orchestrator.Database.GetUserAsync(orchestrator.Subject.Id);
		record.Should().NotBeNull();
		record.Data.AutoMemberHoldRecord.Should().NotBeNull();
		record.Data.AutoMemberHoldRecord.ModeratorID.ID.Should().Be(orchestrator.Actor.Id);
		record.Data.AutoMemberHoldRecord.Reason.Should().Be("Test reason");
	}

	[Fact]
	public static async Task HoldMember_WithNonGuildUser_ShouldGiveError()
	{
		// Arrange
		var orchestrator = TestOrchestrator.Default;
		var cmd = orchestrator.GetCommand<AutoMemberHoldCommand>();

		// Act
		await cmd.Object.HoldMember(new TestUser(orchestrator.Subject), "Test reason");

		// Assert
		cmd.VerifyResponse(Strings.Command_AutoMemberHold_Error_NotGuildMember, ephemeral: true);
	}

	[Fact]
	public static async Task HoldMember_AlreadyAMHed_ShouldGiveError()
	{
		// Arrange
		var orchestrator = TestOrchestrator.Default;
		await orchestrator.CreateAutoMemberHold(orchestrator.Subject);
		var cmd = orchestrator.GetCommand<AutoMemberHoldCommand>();

		// Act
		await cmd.Object.HoldMember(orchestrator.Subject, "Test reason");

		// Assert
		cmd.VerifyResponse(Strings.Command_AutoMemberHold_Error_AMHAlreadyExists, ephemeral: true);
	}

	[Fact]
	public static async Task HoldMember_AlreadyMember_ShouldGiveError()
	{
		// Arrange
		var orchestrator = TestOrchestrator.Default;
		await orchestrator.Subject.AddRoleAsync(orchestrator.Configuration.MemberRoleID);
		var cmd = orchestrator.GetCommand<AutoMemberHoldCommand>();

		// Act
		await cmd.Object.HoldMember(orchestrator.Subject, "Test reason");

		// Assert
		cmd.VerifyResponse(Strings.Command_AutoMemberHold_Error_AlreadyMember, ephemeral: true);
	}

	[Fact]
	public static async Task HoldMember_WithDynamoDBError_ShouldRespondWithError()
	{
		// Arrange
		var orchestrator = TestOrchestrator.Default;
		var cmd = orchestrator.GetCommand<AutoMemberHoldCommand>();

		if (orchestrator.Database is not IMockOf<IDatabaseService> dbMock)
			throw new InvalidOperationException("This test depends on the registered database implementing IMockOf<IDatabaseService>");

		dbMock.Mock.Setup(n => n.GetOrCreateUserAsync(It.Is<IGuildUser>(n => n.Id == orchestrator.Subject.Id))).Throws<BadStateException>();

		// Act
		await cmd.Object.HoldMember(orchestrator.Subject, "Test reason");

		// Assert
		cmd.VerifyResponse(Strings.Command_AutoMemberHold_Error_Unexpected, ephemeral: true);

		var record = await orchestrator.Database.GetUserAsync(orchestrator.Subject.Id);
		record.Should().NotBeNull();
		record.Data.AutoMemberHoldRecord.Should().BeNull();

		var metrics = (TestMetricService) orchestrator.GetService<IMetricService>();
		metrics.GetMetricValues(Metric.AMS_AMHFailures).Sum().Should().BeGreaterThan(0);
	}

	[Fact]
	public static async Task UnholdMember_WithValidUser_ShouldRemoveAMH()
	{
		// Arrange
		var orchestrator = TestOrchestrator.Default;
		await orchestrator.CreateAutoMemberHold(orchestrator.Subject);
		var cmd = orchestrator.GetCommand<AutoMemberHoldCommand>();

		// Act
		await cmd.Object.UnholdMember(orchestrator.Subject);

		// Assert
		cmd.VerifyResponse(Strings.Command_AutoMemberUnhold_Success, ephemeral: true);

		var afterRecord = await orchestrator.Database.GetUserAsync(orchestrator.Subject.Id);
		afterRecord.Should().NotBeNull();
		afterRecord.Data.AutoMemberHoldRecord.Should().BeNull();
	}

	[Fact]
	public static async Task UnholdMember_WithValidUserNoActiveHold_ShouldReturnError()
	{
		// Arrange
		var orchestrator = TestOrchestrator.Default;
		var cmd = orchestrator.GetCommand<AutoMemberHoldCommand>();

		// Act
		await cmd.Object.UnholdMember(orchestrator.Subject);

		// Assert
		cmd.VerifyResponse(Strings.Command_AutoMemberUnhold_Error_NoActiveHold, ephemeral: true);
	}

	[Fact]
	public static async Task UnholdMember_WithNonGuildUser_ShouldReturnError()
	{
		// Arrange
		var orchestrator = TestOrchestrator.Default;
		var cmd = orchestrator.GetCommand<AutoMemberHoldCommand>();

		// Act
		await cmd.Object.UnholdMember(new TestUser(orchestrator.Subject));

		// Assert
		cmd.VerifyResponse(Strings.Command_AutoMemberUnhold_Error_NotGuildMember, ephemeral: true);
	}

	[Fact]
	public static async Task UnholdMember_WithDynamoError_ShouldReturnError()
	{
		// Arrange
		var orchestrator = TestOrchestrator.Default;
		await orchestrator.CreateAutoMemberHold(orchestrator.Subject);
		var cmd = orchestrator.GetCommand<AutoMemberHoldCommand>();

		if (orchestrator.Database is not IMockOf<IDatabaseService> dbMock)
			throw new InvalidOperationException("This test depends on the registered database implementing IMockOf<IDatabaseService>");

		dbMock.Mock.Setup(n => n.GetOrCreateUserAsync(It.Is<IGuildUser>(n => n.Id == orchestrator.Subject.Id))).Throws<BadStateException>();

		// Act
		await cmd.Object.UnholdMember(orchestrator.Subject);

		// Assert
		cmd.VerifyResponse(Strings.Command_AutoMemberUnhold_Error_Unexpected, ephemeral: true);

		// Sanity check: if the DDB errors out, the AMH should still be there
		var afterRecord = await orchestrator.Database.GetUserAsync(orchestrator.Subject.Id);
		afterRecord.Should().NotBeNull();
		afterRecord.Data.AutoMemberHoldRecord.Should().NotBeNull();
	}
}