using Discord;
using FluentAssertions;
using InstarBot.Tests.Models;
using InstarBot.Tests.Services;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Metrics;
using Xunit;

namespace InstarBot.Tests.Integration.Interactions;

public static class AutoMemberSystemCommandTests
{
	private const ulong NewMemberRole = 796052052433698817ul;
	private const ulong MemberRole = 793611808372031499ul;

	private static async Task<Context> Setup(bool setupAMH = false)
	{
		TestUtilities.SetupLogging();

		// This is going to be annoying
		var userID = Snowflake.Generate();
		var modID = Snowflake.Generate();

		var mockDDB = new MockInstarDDBService();
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
			await ddbRecord.UpdateAsync();
		}

		var commandMock = TestUtilities.SetupCommandMock(
			() => new AutoMemberHoldCommand(mockDDB, TestUtilities.GetDynamicConfiguration(), mockMetrics),
			new TestContext
			{
				UserID = modID
			});

		return new Context(mockDDB, mockMetrics, user, mod, commandMock);
	}

	[Fact]
	public static async Task HoldMember_WithValidUserAndReason_ShouldCreateRecord()
	{
		// Arrange
		var ctx = await Setup();

		// Act
		await ctx.Command.Object.HoldMember(ctx.TargetUser, "Test reason");

		// Assert
		TestUtilities.VerifyMessage(ctx.Command, Strings.Command_AutoMemberHold_Success, ephemeral: true);

		var record = await ctx.DDBService.GetUserAsync(ctx.TargetUser.Id);
		record.Should().NotBeNull();
		record.Data.AutoMemberHoldRecord.Should().NotBeNull();
		record.Data.AutoMemberHoldRecord.ModeratorID.ID.Should().Be(ctx.Moderator.Id);
		record.Data.AutoMemberHoldRecord.Reason.Should().Be("Test reason");
	}

	[Fact]
	public static async Task HoldMember_WithNonGuildUser_ShouldGiveError()
	{
		// Arrange
		var ctx = await Setup();

		// Act
		await ctx.Command.Object.HoldMember(new TestUser(ctx.TargetUser), "Test reason");

		// Assert
		TestUtilities.VerifyMessage(ctx.Command, Strings.Command_AutoMemberHold_Error_NotGuildMember, ephemeral: true);
	}

	[Fact]
	public static async Task HoldMember_AlreadyAMHed_ShouldGiveError()
	{
		// Arrange
		var ctx = await Setup(true);

		// Act
		await ctx.Command.Object.HoldMember(ctx.TargetUser, "Test reason");

		// Assert
		TestUtilities.VerifyMessage(ctx.Command, Strings.Command_AutoMemberHold_Error_AMHAlreadyExists, ephemeral: true);
	}

	[Fact]
	public static async Task HoldMember_AlreadyMember_ShouldGiveError()
	{
		// Arrange
		var ctx = await Setup();
		await ctx.TargetUser.AddRoleAsync(MemberRole);

		// Act
		await ctx.Command.Object.HoldMember(ctx.TargetUser, "Test reason");

		// Assert
		TestUtilities.VerifyMessage(ctx.Command, Strings.Command_AutoMemberHold_Error_AlreadyMember, ephemeral: true);
	}

	[Fact]
	public static async Task HoldMember_WithDynamoDBError_ShouldRespondWithError()
	{
		// Arrange
		var ctx = await Setup();

		// Act
		ctx.DDBService
			.Setup(n => n.GetOrCreateUserAsync(It.IsAny<IGuildUser>()))
			.ThrowsAsync(new BadStateException());

		await ctx.Command.Object.HoldMember(ctx.TargetUser, "Test reason");

		// Assert
		TestUtilities.VerifyMessage(ctx.Command, Strings.Command_AutoMemberHold_Error_Unexpected, ephemeral: true);

		var record = await ctx.DDBService.GetUserAsync(ctx.TargetUser.Id);
		record.Should().NotBeNull();
		record.Data.AutoMemberHoldRecord.Should().BeNull();
		ctx.Metrics.GetMetricValues(Metric.AMS_AMHFailures).Sum().Should().BeGreaterThan(0);
	}

	[Fact]
	public static async Task UnholdMember_WithValidUser_ShouldRemoveAMH()
	{
		// Arrange
		var ctx = await Setup(true);

		// Act
		await ctx.Command.Object.UnholdMember(ctx.TargetUser);

		// Assert
		TestUtilities.VerifyMessage(ctx.Command, Strings.Command_AutoMemberUnhold_Success, ephemeral: true);

		var afterRecord = await ctx.DDBService.GetUserAsync(ctx.TargetUser.Id);
		afterRecord.Should().NotBeNull();
		afterRecord.Data.AutoMemberHoldRecord.Should().BeNull();
	}

	[Fact]
	public static async Task UnholdMember_WithValidUserNoActiveHold_ShouldReturnError()
	{
		// Arrange
		var ctx = await Setup();

		// Act
		await ctx.Command.Object.UnholdMember(ctx.TargetUser);

		// Assert
		TestUtilities.VerifyMessage(ctx.Command, Strings.Command_AutoMemberUnhold_Error_NoActiveHold, ephemeral: true);
	}

	[Fact]
	public static async Task UnholdMember_WithNonGuildUser_ShouldReturnError()
	{
		// Arrange
		var ctx = await Setup();

		// Act
		await ctx.Command.Object.UnholdMember(new TestUser(ctx.TargetUser));

		// Assert
		TestUtilities.VerifyMessage(ctx.Command, Strings.Command_AutoMemberUnhold_Error_NotGuildMember, ephemeral: true);
	}

	[Fact]
	public static async Task UnholdMember_WithDynamoError_ShouldReturnError()
	{
		// Arrange
		var ctx = await Setup(true);

		ctx.DDBService
			.Setup(n => n.GetOrCreateUserAsync(It.IsAny<IGuildUser>()))
			.ThrowsAsync(new BadStateException());

		// Act
		await ctx.Command.Object.UnholdMember(ctx.TargetUser);

		// Assert
		TestUtilities.VerifyMessage(ctx.Command, Strings.Command_AutoMemberUnhold_Error_Unexpected, ephemeral: true);

		// Sanity check: if the DDB errors out, the AMH should still be there
		var afterRecord = await ctx.DDBService.GetUserAsync(ctx.TargetUser.Id);
		afterRecord.Should().NotBeNull();
		afterRecord.Data.AutoMemberHoldRecord.Should().NotBeNull();
	}

	private record Context(
		MockInstarDDBService DDBService,
		MockMetricService Metrics,
		TestGuildUser TargetUser,
		TestGuildUser Moderator,
		Mock<AutoMemberHoldCommand> Command);
}