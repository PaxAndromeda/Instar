using Discord;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.DynamoModels;

namespace PaxAndromeda.Instar.Embeds;

public class NotificationEmbed(Notification notification, IGuildUser? actor, InstarDynamicConfiguration cfg)
	: InstarEmbed
{
	public override Embed Build()
	{
		var fields = new List<EmbedFieldBuilder>();

		if (notification.Data.Fields is not null)
		{
			fields.AddRange(notification.Data.Fields.Select(data =>
			{
				var builder = new EmbedFieldBuilder()
				.WithName(data.Name)
				.WithValue(data.Value);

				if (data.Inline is true)
					builder = builder.WithIsInline(true);

				return builder;
			}));
		}

		var builder = new EmbedBuilder()
            // Set up all the basic stuff first
            .WithTimestamp(notification.Date)
			.WithColor(0x0c94e0)
			.WithFooter(new EmbedFooterBuilder()
				.WithText(Strings.Embed_Notification_Footer)
				.WithIconUrl(Strings.InstarLogoUrl))
            // Description
			.WithTitle(notification.Subject)
            .WithDescription(notification.Data.Message)
			.WithFields(fields);

		builder = actor is not null
			? builder.WithAuthor(actor.DisplayName, actor.GetDisplayAvatarUrl())
			: builder.WithAuthor(cfg.BotName, Strings.InstarLogoUrl);

		if (notification.Data.Url is not null)
			builder = builder.WithUrl(notification.Data.Url);
		if (notification.Data.ImageUrl is not null)
			builder = builder.WithImageUrl(notification.Data.ImageUrl);
		if (notification.Data.ThumbnailUrl is not null)
			builder = builder.WithThumbnailUrl(notification.Data.ThumbnailUrl);

		return builder.Build();
	}
}