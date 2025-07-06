namespace PaxAndromeda.Instar;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class SnowflakeTypeAttribute(SnowflakeType type) : Attribute
{
    public SnowflakeType Type { get; } = type;
}