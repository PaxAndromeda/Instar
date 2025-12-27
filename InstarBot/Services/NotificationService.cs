using Discord;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Embeds;
using PaxAndromeda.Instar.Metrics;
using Serilog;
using System.Text;

namespace PaxAndromeda.Instar.Services;

public interface INotificationService : IScheduledService
{
	Task QueueNotification(Notification notification, TimeSpan delay);
}

public class NotificationService (
	TimeProvider timeProvider,
	IMetricService metricService,
	IDatabaseService dbService,
	IDiscordService discordService,
	IDynamicConfigService dynamicConfig)
	: ScheduledService("* * * * *", timeProvider, metricService, "Notifications Service"), INotificationService
{
	private readonly TimeProvider _timeProvider = timeProvider;
	private readonly IMetricService _metricService = metricService;

	internal override Task Initialize()
	{
		return Task.CompletedTask;
	}

	public override async Task RunAsync()
	{
		var notificationQueue = new Queue<InstarDatabaseEntry<Notification>>(
			(await dbService.GetPendingNotifications()).OrderByDescending(entry => entry.Data.Date)
			);

		var cfg = await dynamicConfig.GetConfig();

		await discordService.SyncUsers();

		while (notificationQueue.TryDequeue(out var notification))
		{
			if (!await ProcessNotification(notification, cfg))
				continue;

			await notification.DeleteAsync();
		}
	}

	private async Task<bool> ProcessNotification(InstarDatabaseEntry<Notification> notification, InstarDynamicConfiguration cfg)
	{
		try
		{
			Log.Information("Processed notification from {Actor} sent on {Date}: {Message}", notification.Data.Actor.ID, notification.Data.Date, notification.Data.Data.Message);

			IChannel? channel = await discordService.GetChannel(notification.Data.Channel);
			if (channel is not ITextChannel textChannel)
			{
				Log.Error("Failed to send notification dated {NotificationDate}: channel {ChannelId} does not exist", notification.Data.Date, notification.Data.Channel);
				await _metricService.Emit(Metric.Notification_MalformedNotification, 1);
				return true;
			}

			var actor = discordService.GetUser(notification.Data.Actor);

			Log.Debug("Actor ID {UserId} name: {Username}", notification.Data.Actor.ID, actor?.Username ?? "<unknown>");
			
			var embed = new NotificationEmbed(notification.Data, actor, cfg);

			await textChannel.SendMessageAsync(GetNotificationTargetString(notification.Data), embed: embed.Build());
			await _metricService.Emit(Metric.Notification_NotificationsSent, 1);

			return true;
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to send notification dated {NotificationDate}: an unknown error has occurred", notification.Data.Date);
			await _metricService.Emit(Metric.Notification_NotificationsFailed, 1);

			try
			{
				// Let's try to mark the notification for a reattempt
				notification.Data.SendAttempts++;
				await notification.CommitAsync();
			} catch (Exception ex2)
			{
				Log.Error(ex2, "Failed to update notification dated {NotificationDate}: cannot increment send attempts", notification.Data.Date);
			}
		}

		return false;
	}

	private static string GetNotificationTargetString(Notification notificationData)
	{
		return notificationData.Targets.Count switch
		{
			1 => GetMention(notificationData.Targets.First()),
			>= 2 => string.Join(' ', notificationData.Targets.Select(GetMention)).TrimEnd(),
			_ => string.Empty
		};
	}

	private static string GetMention(NotificationTarget target)
	{
		StringBuilder builder = new();
		builder.Append(target.Type switch
		{
			NotificationTargetType.User => "<@",
			NotificationTargetType.Role => "<@&",
			_ => "<@"
		});
		
		builder.Append(target.Id.ID);
		builder.Append(">");

		return builder.ToString();
	}

	public async Task QueueNotification(Notification notification, TimeSpan delay)
	{
		var cfg = await dynamicConfig.GetConfig();

		notification.Date = _timeProvider.GetUtcNow().UtcDateTime + delay;
		notification.GuildID = cfg.TargetGuild;

		var notificationEntry = await dbService.CreateNotificationAsync(notification);
		await notificationEntry.CommitAsync();
	}
}