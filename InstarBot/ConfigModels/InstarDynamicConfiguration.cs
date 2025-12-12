// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMember.Global

using JetBrains.Annotations;

namespace PaxAndromeda.Instar.ConfigModels;

public sealed class InstarDynamicConfiguration
{
    public string BotName { get; set; } = null!;
	[SnowflakeType(SnowflakeType.User)] public Snowflake BotUserID { get; set; } = null!;
	[SnowflakeType(SnowflakeType.Guild)] public Snowflake TargetGuild { get; set; } = null!;
    [SnowflakeType(SnowflakeType.Channel)] public Snowflake TargetChannel { get; set; } = null!;
    public string Token { get; set; } = null!;
    public string GaiusAPIKey { get; set; } = null!;
    public DynamicAWSConfig AWS { get; set; } = null!;
    [SnowflakeType(SnowflakeType.Channel)] public Snowflake StaffAnnounceChannel { get; set; } = null!;
    [SnowflakeType(SnowflakeType.Role)] public Snowflake StaffRoleID { get; set; } = null!;
    [SnowflakeType(SnowflakeType.Role)] public Snowflake NewMemberRoleID { get; set; } = null!;
    [SnowflakeType(SnowflakeType.Role)] public Snowflake MemberRoleID { get; set; } = null!;
    public Snowflake[] AuthorizedStaffID { get; set; } = null!;
    public AutoMemberConfig AutoMemberConfig { get; set; } = null!;
	public BirthdayConfig BirthdayConfig { get; set; } = null!;
    public Team[] Teams { get; set; } = null!;
    public Dictionary<string, PhraseCommand> FunCommands { get; set; } = null!;
}

[UsedImplicitly]
public class BirthdayConfig
{
	[SnowflakeType(SnowflakeType.Role)]
	public Snowflake BirthdayRole { get; set; } = null!;
	[SnowflakeType(SnowflakeType.Channel)]
	public Snowflake BirthdayAnnounceChannel { get; set; } = null!;
	public int MinimumPermissibleAge { get; set; }
	public List<AgeRoleMapping> AgeRoleMap { get; set; } = null!;
}

[UsedImplicitly]
public record AgeRoleMapping
{
	public int Age { get; set; }

	[SnowflakeType(SnowflakeType.Role)]
	public Snowflake Role { get; set; } = null!;
}

[UsedImplicitly]
public class DynamicAWSConfig
{
    public string AccessKey { get; set; } = null!;
    public string SecretAccessKey { get; set; } = null!;
    public string Region { get; set; } = null!;
    public DynamicCloudWatchConfig CloudWatch { get; set; } = null!;
    public DynamicAppConfigConfig AppConfig { get; set; } = null!;
}

[UsedImplicitly]
public class DynamicCloudWatchConfig
{
    public bool Enabled { get; set; }
    public string LogGroup { get; set; } = null!;
    public string MetricNamespace { get; set; } = null!;
}

[UsedImplicitly]
public class DynamicAppConfigConfig
{
    public string ParameterNamespace { get; set; } = null!;
}

[UsedImplicitly]
public class RequiredRoles
{
    public string GroupName { get; set; } = null!;
    public Snowflake[] Roles { get; set; } = null!;
}

[UsedImplicitly]
public class PhraseCommand
{
    public string[] Phrases { get; set; } = null!;
}

