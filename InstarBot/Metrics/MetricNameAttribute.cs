using System.Diagnostics.CodeAnalysis;

namespace PaxAndromeda.Instar.Metrics;

[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Field)]
public sealed class MetricNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}