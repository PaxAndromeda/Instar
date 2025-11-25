using Ardalis.GuardClauses;
using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.Embeds;
using PaxAndromeda.Instar.Metrics;
using PaxAndromeda.Instar.Preconditions;
using PaxAndromeda.Instar.Services;
using Serilog;
using System.Diagnostics.CodeAnalysis;

namespace PaxAndromeda.Instar.Commands;

// Required to be unsealed for mocking
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")] // Required for mocking
public class PageCommand(TeamService teamService, IMetricService metricService) : BaseCommand
{
    [UsedImplicitly]
    [SlashCommand("page", "This command initiates a directed page.")]
    [RequireStaffMember]
    // Stupid way to hide this command for unauthorized personnel
    [DefaultMemberPermissions(GuildPermission.MuteMembers)]
    public async Task Page(
        [Summary("team", "The team you wish to page.")]
        PageTarget team,
        [MinLength(12)] [Summary("reason", "The reason for the page.")]
        string reason,
        [Summary("teamlead", "Do you wish to page the team lead for the team you selected?")]
        bool teamLead = false,
        [Summary("message", "A message link related to the reason you're paging.")]
        string message = "",
        [Summary("user", "The user you are paging about.")]
        IUser? user = null,
        [Summary("channel", "The channel you are paging about.")]
        IChannel? channel = null)
    {
        Guard.Against.NullOrEmpty(reason);
        Guard.Against.Null(Context.User);

        try
        {
            Log.Verbose("User {User} is attempting to page {Team}: {Reason}", Context.User.Id, team, reason);

            var userTeam = await teamService.GetUserPrimaryStaffTeam(Context.User);
            if (!CheckPermissions(Context.User, userTeam, team, teamLead, out var response))
            {
                await RespondAsync(response, ephemeral: true);
                return;
            }

            string mention;
            if (team == PageTarget.Test)
                mention = Strings.Command_Page_TestPageMessage;
            else if (teamLead)
                mention = await teamService.GetTeamLeadMention(team);
            else
                mention = await teamService.GetTeamMention(team);

            Log.Debug("Emitting page to {ChannelName}", Context.Channel?.Name);
            await RespondAsync(
                mention,
                embed: new InstarPageEmbed(reason, message, user, channel, userTeam!, Context.User).Build(),
                allowedMentions: AllowedMentions.All);

            await metricService.Emit(Metric.Paging_SentPages, 1);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to send page from {User}", Context.User.Id);
            await RespondAsync(Strings.Command_Page_Error_Unexpected, ephemeral: true);
        }
    }

    /// <summary>
    ///     Determines whether a <paramref name="user" /> has the authority to issue a page to <paramref name="pageTarget" />.
    /// </summary>
    /// <param name="user">The user attempting to issue a page</param>
    /// <param name="team">The user's staff team</param>
    /// <param name="pageTarget">The target the user is attempting to page</param>
    /// <param name="teamLead">Whether the user is attempting to page the team's team leader</param>
    /// <param name="response">A response string to show to the user if they are not authorized to send a page.</param>
    /// <returns>A boolean indicating whether the user has permissions to send this page.</returns>
    private static bool CheckPermissions(IGuildUser user, Team? team, PageTarget pageTarget, bool teamLead,
        out string? response)
    {
        response = null;

        if (team is null)
        {
            response = Strings.Command_Page_Error_NotAuthorized;
            Log.Information("{User} was not authorized to send a page", user.Id);
            return false;
        }

        if (pageTarget == PageTarget.Test)
            return true;

        // Check permissions.  Only mod+ can send an "all" page
        if (team.Priority > 3 && pageTarget == PageTarget.All) // i.e. Helper, Community Manager
        {
            response = Strings.Command_Page_Error_FullTeamNotAuthorized;
            Log.Information("{User} was not authorized to send a page to the entire staff team", user.Id);
            return false;
        }

        if (pageTarget != PageTarget.All || !teamLead)
            return true;

		response = Strings.Command_Page_Error_NoAllTeamlead;

		return false;
    }
}