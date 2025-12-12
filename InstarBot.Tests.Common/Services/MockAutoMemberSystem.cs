using Discord;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.Services;

namespace InstarBot.Tests.Services;

public class MockAutoMemberSystem : IAutoMemberSystem
{
	public Task RunAsync()
	{
		throw new NotImplementedException();
	}

	public virtual MembershipEligibility CheckEligibility(InstarDynamicConfiguration cfg, IGuildUser user)
	{
		throw new NotImplementedException();
	}

	public Task Initialize()
	{
		throw new NotImplementedException();
	}
}