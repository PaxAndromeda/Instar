using Discord;
using FluentAssertions;
using InstarBot.Test.Framework;
using InstarBot.Test.Framework.Models;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Metrics;
using PaxAndromeda.Instar.Services;
using Xunit;

namespace InstarBot.Tests.Integration.Services;

public static class NotificationSystemTests
{
	private static async Task<TestOrchestrator> SetupOrchestrator()
	{
		return await TestOrchestrator.Builder
			.WithService<INotificationService, NotificationService>()
			.Build();
	}

	private static Notification CreateNotification(TestOrchestrator orchestrator)
	{
		return new Notification
		{
			Actor = orchestrator.Actor.Id,
			Channel = orchestrator.Configuration.StaffAnnounceChannel,
			Date = (orchestrator.TimeProvider.GetUtcNow() - TimeSpan.FromMinutes(5)).UtcDateTime,
			GuildID = orchestrator.GuildID,
			Priority = NotificationPriority.Normal,
			Subject = "Some test subject",
			Targets = [
				new NotificationTarget { Id = orchestrator.Configuration.StaffRoleID, Type = NotificationTargetType.Role }
			],
			Data = new NotificationData
			{
				Message = "This is a test notification",
				Fields = [
					new NotificationEmbedField
					{
						Name = "Some field",
						Value = "Some value"
					}
				]
			}
		};
	}

	private static EmbedVerifier CreateVerifierFromNotification(TestOrchestrator orchestrator, Notification notification)
	{
		var verifier = EmbedVerifier.Builder()
			.WithAuthorName(orchestrator.Actor.Username)
			.WithTitle(notification.Subject)
			.WithDescription(notification.Data.Message);

		if (notification.Data.Fields is not null)
			verifier = notification.Data.Fields.Aggregate(verifier, (current, field) => current.WithField(field.Name, field.Value));

		return verifier.Build();
	}

	[Fact]
	public static async Task NotificationSystem_ShouldEmitNothing_WhenNoNotificationsArePresent()
	{
		// Arrange
		var orchestrator = await SetupOrchestrator();
		var service = orchestrator.GetService<INotificationService>();

		// Act
		await service.RunAsync();

		// Assert
		orchestrator.Metrics.GetMetricValues(Metric.Notification_NotificationsSent).Sum().Should().Be(0);
		orchestrator.Metrics.GetMetricValues(Metric.Notification_NotificationsFailed).Sum().Should().Be(0);
		orchestrator.Metrics.GetMetricValues(Metric.Notification_MalformedNotification).Sum().Should().Be(0);
	}

	[Fact]
	public static async Task NotificationSystem_ShouldPostEmbed_WithPendingNotification()
	{
		// Arrange
		var orchestrator = await SetupOrchestrator();
		var service = orchestrator.GetService<INotificationService>();

		if (orchestrator.Actor is not TestGuildUser tgu)
			throw new InvalidOperationException("Actor is not a TestGuildUser");

		tgu.Username = "username";

		var notification = CreateNotification(orchestrator);
		var verifier = CreateVerifierFromNotification(orchestrator, notification);
		var channel = orchestrator.GetChannel(orchestrator.Configuration.StaffAnnounceChannel);

		// Populate the database
		await orchestrator.Database.CreateNotificationAsync(notification);
		(await orchestrator.Database.GetPendingNotifications()).Count.Should().Be(1);

		// Act
		await service.RunAsync();

		// Assert
		channel.VerifyEmbed(verifier, "<@&{0}>");

		orchestrator.Metrics.GetMetricValues(Metric.Notification_NotificationsSent).Sum().Should().Be(1);
		orchestrator.Metrics.GetMetricValues(Metric.Notification_NotificationsFailed).Sum().Should().Be(0);
		orchestrator.Metrics.GetMetricValues(Metric.Notification_MalformedNotification).Sum().Should().Be(0);

		(await orchestrator.Database.GetPendingNotifications()).Count.Should().Be(0);
	}

	[Fact]
	public static async Task NotificationSystem_ShouldDoNothing_WithNotificationPendingForAnotherGuild()
	{
		// Arrange
		var orchestrator = await SetupOrchestrator();
		var service = orchestrator.GetService<INotificationService>();

		var notification = CreateNotification(orchestrator);
		notification.GuildID = Snowflake.Generate();

		// Populate the database
		await orchestrator.Database.CreateNotificationAsync(notification);
		(await orchestrator.Database.GetPendingNotifications()).Count.Should().Be(0);

		// Act
		await service.RunAsync();

		// Assert
		orchestrator.Metrics.GetMetricValues(Metric.Notification_NotificationsSent).Sum().Should().Be(0);
		orchestrator.Metrics.GetMetricValues(Metric.Notification_NotificationsFailed).Sum().Should().Be(0);
		orchestrator.Metrics.GetMetricValues(Metric.Notification_MalformedNotification).Sum().Should().Be(0);

		(await orchestrator.Database.GetPendingNotifications()).Count.Should().Be(0);
	}

	[Fact]
	public static async Task NotificationSystem_ShouldEmitError_WithBadChannel()
	{
		// Arrange
		var orchestrator = await SetupOrchestrator();
		var service = orchestrator.GetService<INotificationService>();

		var notification = CreateNotification(orchestrator);
		// Override the channel
		notification.Channel = new Snowflake(Snowflake.Epoch);


		// Populate the database
		await orchestrator.Database.CreateNotificationAsync(notification);
		(await orchestrator.Database.GetPendingNotifications()).Count.Should().Be(1);

		// Act
		await service.RunAsync();

		// Assert
		orchestrator.Metrics.GetMetricValues(Metric.Notification_NotificationsSent).Sum().Should().Be(0);
		orchestrator.Metrics.GetMetricValues(Metric.Notification_NotificationsFailed).Sum().Should().Be(0);
		orchestrator.Metrics.GetMetricValues(Metric.Notification_MalformedNotification).Sum().Should().Be(1);

		(await orchestrator.Database.GetPendingNotifications()).Count.Should().Be(0);
	}

	[Fact]
	public static async Task NotificationSystem_ShouldEmitError_WithFailedMessageSend()
	{
		// Arrange
		var orchestrator = await SetupOrchestrator();
		var service = orchestrator.GetService<INotificationService>();

		var notification = CreateNotification(orchestrator);

		if (orchestrator.GetChannel(orchestrator.Configuration.StaffAnnounceChannel) is not IMockOf<ITextChannel> textChannelMock)
			throw new InvalidOperationException($"Expected channel {orchestrator.Configuration.StaffAnnounceChannel.ID} to be IMockOf<ITextChannel>");

		textChannelMock.Mock.Setup(n => n.SendMessageAsync(
			It.IsAny<string>(),
			It.IsAny<bool>(),
			It.IsAny<Embed>(),
			It.IsAny<RequestOptions>(),
			It.IsAny<AllowedMentions>(),
			It.IsAny<MessageReference>(),
			It.IsAny<MessageComponent>(),
			It.IsAny<ISticker[]>(),
			It.IsAny<Embed[]>(),
			It.IsAny<MessageFlags>(),
			It.IsAny<PollProperties>())).Throws<BadStateException>();


		// Populate the database
		await orchestrator.Database.CreateNotificationAsync(notification);
		(await orchestrator.Database.GetPendingNotifications()).Count.Should().Be(1);

		// Act
		await service.RunAsync();

		// Assert
		orchestrator.Metrics.GetMetricValues(Metric.Notification_NotificationsSent).Sum().Should().Be(0);
		orchestrator.Metrics.GetMetricValues(Metric.Notification_NotificationsFailed).Sum().Should().Be(1);
		orchestrator.Metrics.GetMetricValues(Metric.Notification_MalformedNotification).Sum().Should().Be(0);

		var pendingNotifications = await orchestrator.Database.GetPendingNotifications();
		pendingNotifications.Count.Should().Be(1);
		pendingNotifications.First().Should().NotBeNull();
		pendingNotifications.First().Data.SendAttempts.Should().Be(1);
	}
}