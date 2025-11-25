using System.Collections.Immutable;
using Discord;
using Serilog;

namespace InstarBot.Tests;

[Flags]
public enum EmbedVerifierMatchFlags
{
	None,
	PartialTitle,
	PartialDescription,
	PartialAuthorName,
	PartialFooterText
}

public class EmbedVerifier
{
	public EmbedVerifierMatchFlags MatchFlags { get; set; } = EmbedVerifierMatchFlags.None;

	public string? Title { get; set; }
	public string? Description { get; set; }

	public string? AuthorName { get; set; }
	public string? FooterText { get; set; }

	private readonly List<(string?, string, bool)> _fields = [];

	public void AddField(string name, string value, bool partial = false)
	{
		_fields.Add((name, value, partial));
	}

	public void AddFieldValue(string value, bool partial = false)
	{
		_fields.Add((null, value, partial));
	}

	public bool Verify(Embed embed)
	{
		if (!VerifyString(Title, embed.Title, MatchFlags.HasFlag(EmbedVerifierMatchFlags.PartialTitle)))
		{
			Log.Error("Failed to match title:  Expected '{Expected}', got '{Actual}'", Title, embed.Title);
			return false;
		}

		if (!VerifyString(Description, embed.Description, MatchFlags.HasFlag(EmbedVerifierMatchFlags.PartialDescription)))
		{
			Log.Error("Failed to match description:  Expected '{Expected}', got '{Actual}'", Description, embed.Description);
			return false;
		}

		if (!VerifyString(AuthorName, embed.Author?.Name, MatchFlags.HasFlag(EmbedVerifierMatchFlags.PartialAuthorName)))
		{
			Log.Error("Failed to match author name:  Expected '{Expected}', got '{Actual}'", AuthorName, embed.Author?.Name);
			return false;
		}

		if (!VerifyString(FooterText, embed.Footer?.Text, MatchFlags.HasFlag(EmbedVerifierMatchFlags.PartialFooterText)))
		{
			Log.Error("Failed to match footer text:  Expected '{Expected}', got '{Actual}'", FooterText, embed.Footer?.Text);
			return false;
		}


		return VerifyFields(embed.Fields);
	}

	private bool VerifyFields(ImmutableArray<EmbedField> embedFields)
	{
		foreach (var (name, value, partial) in _fields)
		{
			if (!embedFields.Any(n => VerifyString(name, n.Name, partial) && VerifyString(value, n.Value, partial)))
			{
				Log.Error("Failed to match field:  Expected Name '{ExpectedName}', Value '{ExpectedValue}'", name, value);
				return false;
			}
		}

		return true;
	}

	private static bool VerifyString(string? expected, string? actual, bool partial = false)
	{
		if (expected is null)
			return true;
		if (actual is null)
			return false;


		if (expected.Contains("{") && expected.Contains("}"))
			return TestUtilities.MatchesFormat(actual, expected, partial);

		return partial ? actual.Contains(expected, StringComparison.Ordinal) : actual.Equals(expected, StringComparison.Ordinal);
	}

	public static VerifierBuilder Builder() => VerifierBuilder.Create();

	public sealed class VerifierBuilder
	{
		private readonly EmbedVerifier _verifier = new();

		public static VerifierBuilder Create() => new();

		public VerifierBuilder WithFlags(EmbedVerifierMatchFlags flags)
		{
			_verifier.MatchFlags = flags;
			return this;
		}

		public VerifierBuilder WithTitle(string? title)
		{
			_verifier.Title = title;
			return this;
		}

		public VerifierBuilder WithDescription(string? description)
		{
			_verifier.Description = description;
			return this;
		}

		public VerifierBuilder WithAuthorName(string? authorName)
		{
			_verifier.AuthorName = authorName;
			return this;
		}

		public VerifierBuilder WithFooterText(string? footerText)
		{
			_verifier.FooterText = footerText;
			return this;
		}

		public VerifierBuilder WithField(string name, string value, bool partial = false)
		{
			_verifier.AddField(name, value, partial);
			return this;
		}

		public VerifierBuilder WithField(string value, bool partial = false)
		{
			_verifier.AddFieldValue(value, partial);
			return this;
		}

		public EmbedVerifier Build() => _verifier;
	}
}