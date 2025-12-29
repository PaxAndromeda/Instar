using System.Diagnostics.CodeAnalysis;

namespace PaxAndromeda.Instar;

[ExcludeFromCodeCoverage]
public sealed class ConfigurationException(string message) : Exception(message);