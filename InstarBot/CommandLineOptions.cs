using CommandLine;
using JetBrains.Annotations;
using Serilog.Events;

namespace PaxAndromeda.Instar;

[UsedImplicitly]
public class CommandLineOptions
{
	[Option('c', "config-path", Required = false, HelpText = "Sets the configuration path.")]
	[UsedImplicitly]
	public string? ConfigPath { get; set; }


	[Option('l', "level", Required = false, Default = LogEventLevel.Information, HelpText = "Sets the log verbosity")]
	[UsedImplicitly]
	public LogEventLevel? LogLevel { get; set; }
}