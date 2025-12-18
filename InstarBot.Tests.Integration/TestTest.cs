using Discord;
using FluentAssertions;
using InstarBot.Test.Framework;
using InstarBot.Test.Framework.Models;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.Services;
using Xunit;

namespace InstarBot.Tests.Integration;

public class TestTest
{
	private const ulong NewMemberRole = 796052052433698817ul;

	private static async Task<(TestOrchestrator, Mock<SetBirthdayCommand>)> SetupMocks2(DateTimeOffset? timeOverride = null, bool throwError = false)
	{
		var orchestrator = await TestOrchestrator.Builder
			.WithTime(timeOverride)
			.WithService<IBirthdaySystem, BirthdaySystem>()
			.Build();

		await orchestrator.Actor.AddRoleAsync(NewMemberRole);

		// yoooo, this is going to be so nice if this all works
		var cmd = orchestrator.GetCommand<SetBirthdayCommand>();

		if (throwError)
		{
			if (orchestrator.GetService<IDatabaseService>() is not IMockOf<IDatabaseService> ddbService)
				throw new InvalidOperationException("IDatabaseService was not mocked correctly.");

			ddbService.Mock.Setup(n => n.GetOrCreateUserAsync(It.IsAny<IGuildUser>())).Throws<BadStateException>();
		}

		return (orchestrator, cmd);
	}

	[Fact]
	public async Task Test()
	{
		var (orchestrator, mock) = await SetupMocks2(DateTimeOffset.UtcNow);

		var guildUser = orchestrator.Actor as IGuildUser;

		guildUser.RoleIds.Should().Contain(NewMemberRole);
	}
}