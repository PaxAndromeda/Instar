using System.Text.RegularExpressions;
using Serilog;
using Serilog.Events;

namespace InstarBot.Tests;

public static class TestUtilities
{
    public static void SetupLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .WriteTo.Console()
            .CreateLogger();
        Log.Warning("Logging is enabled for this unit test.");
    }

    /// <summary>
    /// Returns true if <paramref name="text"/> matches the format specified in <paramref name="format"/>.
    /// </summary>
    /// <param name="text">The text to validate.</param>
    /// <param name="format">The format to check the text against.</param>
    /// <param name="partial">Allows for partial matching.</param>
    /// <returns>True if the <paramref name="text"/> matches the format in <paramref name="format"/>.</returns>
    public static bool MatchesFormat(string text, string format, bool partial = false)
	{
		string formatRegex = Regex.Escape(format);
		
		if (!partial)
			formatRegex = $"^{formatRegex}$";

		// We cannot simply replace the escaped template variables, as that would escape the braces.
		formatRegex = formatRegex.Replace("\\{", "{").Replace("\\}", "}");

		// Replaces any template variable (e.g., {0}, {name}, etc.) with a regex wildcard that matches any text
		// that is not the original template itself.
		formatRegex = Regex.Replace(
			formatRegex,
			"{(.+?)}",
			m => "(?:(?!\\{" + Regex.Escape(m.Groups[1].Value) +"\\}).+?)"
			);

		return Regex.IsMatch(text, formatRegex);
	}
}