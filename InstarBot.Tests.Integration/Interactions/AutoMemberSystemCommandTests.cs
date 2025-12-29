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

		if (orchestrator.Database is not TestDatabaseService tds)
			throw new InvalidOperationException("Expected orchestrator.Database to be TestDatabaseService!");

		var notifications = tds.GetAllNotifications();
		notifications.Should().ContainSingle();

		var notification = notifications.First();
		notification.Should().NotBeNull();

		notification.Type.Should().Be(NotificationType.AutoMemberHold);
		notification.ReferenceUser!.ID.Should().Be(orchestrator.Subject.Id);
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

		dbMock.Mock.Setup(n => n.GetOrCreateUserAsync(It.Is<IGuildUser>(guildUser => guildUser.Id == orchestrator.Subject.Id))).Throws<BadStateException>();

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

		if (orchestrator.Database is not TestDatabaseService tds)
			throw new InvalidOperationException("Expected orchestrator.Database to be TestDatabaseService!");

		await tds.CreateNotificationAsync(new Notification
		{
			Type = NotificationType.AutoMemberHold,
			ReferenceUser = orchestrator.Subject.Id,
			Date = (orchestrator.TimeProvider.GetUtcNow() + TimeSpan.FromDays(7)).DateTime
		});

		tds.GetAllNotifications().Should().ContainSingle();

		// Act
		await cmd.Object.UnholdMember(orchestrator.Subject);

		// Assert
		cmd.VerifyResponse(Strings.Command_AutoMemberUnhold_Success, ephemeral: true);

		var afterRecord = await orchestrator.Database.GetUserAsync(orchestrator.Subject.Id);
		afterRecord.Should().NotBeNull();
		afterRecord.Data.AutoMemberHoldRecord.Should().BeNull();

		// There is a potential asynchronous delay here, so let's keep waiting for this condition for 5 seconds.
		await Task.WhenAny(
			Task.Delay(5000),
			Task.Factory.StartNew(async () =>
			{
				while (true)
				{
					if (tds.GetAllNotifications().Count == 0)
						break;

					// only poll once every 50ms
					await Task.Delay(50);
				}
			}));
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

		dbMock.Mock.Setup(n => n.GetOrCreateUserAsync(It.Is<IGuildUser>(guildUser => guildUser.Id == orchestrator.Subject.Id))).Throws<BadStateException>();

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