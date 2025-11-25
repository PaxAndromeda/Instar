using Discord;

namespace PaxAndromeda.Instar.Embeds;

public abstract class InstarEmbed
{
	public const string InstarLogoUrl = "https://spacegirl.s3.us-east-1.amazonaws.com/instar.png";

	public abstract Embed Build();
}