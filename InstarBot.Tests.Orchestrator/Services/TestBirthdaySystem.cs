using Discord;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Services;

namespace InstarBot.Test.Framework.Services;

public class TestBirthdaySystem : IBirthdaySystem
{
	public Task Start()
	{
		return Task.CompletedTask;
	}

	public Task RunAsync()
	{
		return Task.CompletedTask;
	}

	public Task GrantUnexpectedBirthday(IGuildUser user, Birthday birthday)
	{
		return Task.CompletedTask;
	}
}