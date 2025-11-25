using FluentAssertions;
using InstarBot.Tests.Models;
using InstarBot.Tests.Services;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.DynamoModels;
using Xunit;

namespace InstarBot.Tests;

public class MockInstarDDBServiceTests
{
	[Fact]
	public static async Task UpdateMember_ShouldPersist()
	{
		// Arrange
		var mockDDB = new MockInstarDDBService();
		var userId = Snowflake.Generate();

		var user = new TestGuildUser
		{
			Id = userId,
			Username = "username",
			JoinedAt = DateTimeOffset.UtcNow
		};

		await mockDDB.CreateUserAsync(InstarUserData.CreateFrom(user));

		// Act
		var retrievedUserEntry = await mockDDB.GetUserAsync(userId);
		retrievedUserEntry.Should().NotBeNull();
		retrievedUserEntry.Data.AutoMemberHoldRecord = new AutoMemberHoldRecord
		{
			Date = DateTime.UtcNow,
			ModeratorID = Snowflake.Generate(),
			Reason = "test reason"
		};
		await retrievedUserEntry.UpdateAsync();

		// Assert
		var newlyRetrievedUserEntry = await mockDDB.GetUserAsync(userId);

		newlyRetrievedUserEntry.Should().NotBeNull();
		newlyRetrievedUserEntry.Data.AutoMemberHoldRecord.Should().NotBeNull();
	}
}