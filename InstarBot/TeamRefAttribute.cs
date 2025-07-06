namespace PaxAndromeda.Instar;

/// <summary>
/// Attribute to indicate the common internal reference for a particular team,
/// regardless of the team's snowflake.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class TeamRefAttribute(string teamInternalId) : Attribute
{
    public string InternalID { get; } = teamInternalId;
}