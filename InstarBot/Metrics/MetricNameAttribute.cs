namespace PaxAndromeda.Instar.Metrics;

[AttributeUsage(AttributeTargets.Field)]
public sealed class MetricNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}