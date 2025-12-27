using Discord;
using JetBrains.Annotations;

namespace PaxAndromeda.Instar.Embeds;

public abstract class InstarEmbed
{
	public const string InstarLogoUrl = "https://spacegirl.s3.us-east-1.amazonaws.com/instar.png";

	[UsedImplicitly]
	public abstract Embed Build();
}