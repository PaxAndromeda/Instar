using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace PaxAndromeda.Instar.ConfigModels;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
[UsedImplicitly]
public sealed class AutoMemberConfig
{
    [SnowflakeType(SnowflakeType.Role)] public Snowflake HoldRole { get; init; } = null!;
    [SnowflakeType(SnowflakeType.Channel)] public Snowflake IntroductionChannel { get; init; } = null!;
    public int MinimumJoinAge { get; init; }
    public int MinimumMessages { get; init; }
    public int MinimumMessageTime { get; init; }
    public List<RoleGroup> RequiredRoles { get; init; } = null!;
    public bool EnableGaiusCheck { get; init; }
}

[UsedImplicitly]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class RoleGroup
{
    [UsedImplicitly] public string GroupName { get; set; } = null!;
    public List<Snowflake> Roles { get; set; } = null!;
}