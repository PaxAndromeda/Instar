using Discord;
using FluentAssertions;
using Moq;
using PaxAndromeda.Instar;
using System;
using System.Threading;
using PaxAndromeda.Instar.Services;

namespace InstarBot.Test.Framework.Models;

public class TestInteractionContext : InstarContext, IMockOf<InstarContext>
{
	private readonly TestOrchestrator _orchestrator;
	public Mock<InstarContext> Mock { get; } = new();

	public IDiscordClient Client => Mock.Object.Client;

	public TestInteractionContext(TestOrchestrator orchestrator, Snowflake actorId, Snowflake channelId)
	{
		_orchestrator = orchestrator;

		var discordService = orchestrator.GetService<IDiscordService>();
		if (discordService.GetUser(actorId) is not { } actor)
			throw new InvalidOperationException("Actor needs to be registered before creating an interaction context");


		Mock.SetupGet(static n => n.User!)
			.Returns(orchestrator.GetService<IDiscordService>().GetUser(actorId) 
			?? throw new InvalidOperationException("Failed to mock interaction context correctly: missing actor"));

		Mock.SetupGet(static n => n.Channel!)
			.Returns(orchestrator.GetService<IDiscordService>().GetChannel(channelId).Result as TestChannel
			?? throw new InvalidOperationException("Failed to mock interaction context correctly: missing channel"));

		Mock.SetupGet(static n => n.Guild).Returns(orchestrator.GetService<IDiscordService>().GetGuild());
	}

	private Mock<IInstarGuild> SetupGuildMock()
	{
		var discordService = _orchestrator.GetService<IDiscordService>();

		var guildMock = new Mock<IInstarGuild>();
		guildMock.Setup(n => n.Id).Returns(_orchestrator.GuildID);
		guildMock.Setup(n => n.GetTextChannel(It.IsAny<ulong>()))
			.Returns((ulong x) => discordService.GetChannel(x) as ITextChannel);

		return guildMock;
	}

	private TestGuildUser SetupUser(IGuildUser user)
	{
		return new TestGuildUser(user.Id, user.RoleIds.Select(id => new Snowflake(id)));
	}

	private TestChannel SetupChannel(Snowflake channelId)
	{
		var discordService = _orchestrator.GetService<IDiscordService>();
		var channel = discordService.GetChannel(channelId).Result;

		if (channel is not TestChannel testChannel)
			throw new InvalidOperationException("Channel must be registered before use in an interaction context");

		return testChannel;
	}


	protected internal override IInstarGuild Guild => Mock.Object.Guild;
	protected internal override IGuildChannel Channel => Mock.Object.Channel;
	protected internal override IGuildUser User => Mock.Object.User;
	public IDiscordInteraction Interaction { get; } = new Mock<IDiscordInteraction>().Object;
}