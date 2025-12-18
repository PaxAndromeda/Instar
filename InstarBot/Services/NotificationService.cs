using PaxAndromeda.Instar.DynamoModels;
using Serilog;

namespace PaxAndromeda.Instar.Services;

public class NotificationService : ScheduledService
{
	private readonly IDatabaseService _dbService;
	private readonly IDiscordService _discordService;
	private readonly IDynamicConfigService _dynamicConfig;

	public NotificationService(
		TimeProvider timeProvider,
		IMetricService metricService,
		IDatabaseService dbService,
		IDiscordService discordService,
		IDynamicConfigService dynamicConfig)
		: base("* * * * *", timeProvider, metricService, "Notifications Service")
	{
		_dbService = dbService;
		_discordService = discordService;
		_dynamicConfig = dynamicConfig;
	}

	internal override Task Initialize()
	{
		return Task.CompletedTask;
	}

	public override async Task RunAsync()
	{
		var notificationQueue = new Queue<InstarDatabaseEntry<Notification>>(
			(await _dbService.GetPendingNotifications()).OrderByDescending(entry => entry.Data.Date)
			);

		while (notificationQueue.TryDequeue(out var notification))
		{
			if (!await ProcessNotification(notification))
				continue;

			await notification.DeleteAsync();
		}
	}

	private async Task<bool> ProcessNotification(InstarDatabaseEntry<Notification> notification)
	{
		Log.Information("Processed notification from {Actor} sent on {Date}: {Message}", notification.Data.Actor.ID, notification.Data.Date, notification.Data.Data.Message);
		return true;
	}
}