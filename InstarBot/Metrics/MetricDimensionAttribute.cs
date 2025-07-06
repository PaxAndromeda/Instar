namespace PaxAndromeda.Instar.Metrics;

[AttributeUsage(AttributeTargets.Field)]
public sealed class MetricDimensionAttribute(string name, string value) : Attribute
{
    public string Name { get; } = name;
    public string Value { get; } = value;
}