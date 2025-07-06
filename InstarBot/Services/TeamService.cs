using System.Diagnostics.CodeAnalysis;
using Discord;
using PaxAndromeda.Instar.ConfigModels;
using Serilog;

namespace PaxAndromeda.Instar.Services;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public sealed class TeamService(IDynamicConfigService dynamicConfig)
{
    public async Task<bool> Exists(string teamRef)
    {
        var cfg = await dynamicConfig.GetConfig();
        
        return cfg.Teams.Any(n => n.InternalID.Equals(teamRef, StringComparison.Ordinal));
    }

    public async Task<bool> Exists(Snowflake snowflake)
    {
        var cfg = await dynamicConfig.GetConfig();
        
        return cfg.Teams.Any(n => n.ID.ID == snowflake.ID);
    }

    public async Task<Team> Get(string teamRef)
    {
        var cfg = await dynamicConfig.GetConfig();
        
        return cfg.Teams.First(n => n.InternalID.Equals(teamRef, StringComparison.Ordinal));
    }

    public async Task<Team> Get(Snowflake snowflake)
    {
        var cfg = await dynamicConfig.GetConfig();
        
        return cfg.Teams.First(n => n.ID.ID == snowflake.ID);
    }

    public async IAsyncEnumerable<Team> GetTeams(PageTarget pageTarget)
    {
        var cfg = await dynamicConfig.GetConfig();
        var teamRefs = pageTarget.GetAttributesOfType<TeamRefAttribute>()?.Select(n => n.InternalID) ??
                       new List<string>();

        foreach (var internalId in teamRefs)
        {
            
            yield return cfg.Teams.First(n => n.InternalID.Equals(internalId, StringComparison.Ordinal));
        }
    }

    public async Task<string> GetTeamLeadMention(PageTarget pageTarget)
    {
        return string.Join(' ', await GetTeams(pageTarget).Select(n => Snowflake.GetMention(() => n.Teamleader)).ToArrayAsync());
    }

    public async Task<string> GetTeamMention(PageTarget pageTarget)
    {
        return string.Join(' ', await GetTeams(pageTarget).Select(n => Snowflake.GetMention(() => n.ID)).ToArrayAsync());
    }

    /// <summary>
    ///     Determines the <paramref name="user" />'s highest staff team, if they are staff.
    /// </summary>
    /// <param name="user">The user in question</param>
    /// <returns>The user's highest staff team, or null if the user is not staff.</returns>
    public async Task<Team?> GetUserPrimaryStaffTeam(IGuildUser user)
    {
        Team? highestTeam = null;
        Log.Debug("User roles: {Roles}", string.Join(", ", user.RoleIds));
        foreach (var role in user.RoleIds.Select(n => new Snowflake(n)))
        {
            if (!await Exists(role))
                continue;

            var st = await Get(role);

            Log.Debug("Team role found: {Role} with internal ID {InternalID}", role.ID, st.InternalID);

            // Set the team if it is null
            highestTeam ??= st;
            if (st.Priority < highestTeam.Priority)
                highestTeam = st;
        }

        if (highestTeam is not null)
            Log.Debug("Highest team: {TeamID} {TeamName}", highestTeam.ID.ID, highestTeam.Name);
        else Log.Debug("Highest team was not found");

        return highestTeam;
    }
}