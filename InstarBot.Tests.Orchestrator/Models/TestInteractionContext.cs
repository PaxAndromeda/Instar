using Discord;
using JetBrains.Annotations;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Services;

namespace InstarBot.Test.Framework.Models;

public class TestInteractionContext : InstarContext, IMockOf<InstarContext>
{
	public Mock<InstarContext> Mock { get; } = new();

	public new IDiscordClient Client => Mock.Object.Client;

	public TestInteractionContext(TestOrchestrator orchestrator, Snowflake actorId, Snowflake channelId)
	{
		var discordService = orchestrator.GetService<IDiscordService>();
		if (discordService.GetUser(actorId) is null)
			throw new InvalidOperationException("Actor needs to be registered before creating an interaction context");


		Mock.SetupGet(static n => n.User!)
			.Returns(orchestrator.GetService<IDiscordService>().GetUser(actorId) 
			?? throw new InvalidOperationException("Failed to mock interaction context correctly: missing actor"));

		Mock.SetupGet(static n => n.Channel!)
			.Returns(orchestrator.GetService<IDiscordService>().GetChannel(channelId).Result as TestChannel
			?? throw new InvalidOperationException("Failed to mock interaction context correctly: missing channel"));

		Mock.SetupGet(static n => n.Guild).Returns(orchestrator.GetService<IDiscordService>().GetGuild());
	}

	protected internal override IInstarGuild Guild => Mock.Object.Guild;
	protected internal override IGuildChannel? Channel => Mock.Object.Channel;
	protected internal override IGuildUser? User => Mock.Object.User;
	[UsedImplicitly] public new IDiscordInteraction Interaction { get; } = new Mock<IDiscordInteraction>().Object;
}