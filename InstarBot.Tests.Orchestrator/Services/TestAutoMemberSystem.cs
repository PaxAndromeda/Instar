using Discord;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.Services;

namespace InstarBot.Test.Framework.Services;

public class TestAutoMemberSystem : IMockOf<IAutoMemberSystem>, IAutoMemberSystem
{
	public Mock<IAutoMemberSystem> Mock { get; } = new();

	public Task Start()
	{
		return Mock.Object.Start();
	}

	public Task RunAsync()
	{
		return Mock.Object.RunAsync();
	}

	public MembershipEligibility CheckEligibility(InstarDynamicConfiguration cfg, IGuildUser user)
	{
		return Mock.Object.CheckEligibility(cfg, user);
	}
}