using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using PaxAndromeda.Instar.Services;

namespace PaxAndromeda.Instar.Commands;

public class TriggerBirthdaySystemCommand(IBirthdaySystem birthdaySystem) : BaseCommand
{
	[UsedImplicitly]
	[RequireOwner]
	[DefaultMemberPermissions(GuildPermission.Administrator)]
	[SlashCommand("runbirthdays", "Manually triggers an auto member system run.")]
	public async Task RunBirthdays()
	{
		await RespondAsync("Auto Member System is running!", ephemeral: true);

		// Run it asynchronously
		await birthdaySystem.RunAsync();
	}
}