using Amazon.DynamoDBv2.DataModel;
using Discord;
using JetBrains.Annotations;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Services;
using System.Diagnostics.CodeAnalysis;

namespace InstarBot.Test.Framework.Services;

[UsedImplicitly]
public class TestDatabaseService : IMockOf<IDatabaseService>, IDatabaseService
{
	private readonly IDynamicConfigService _dynamicConfig;
	private readonly TimeProvider _timeProvider;
	private readonly Dictionary<Snowflake, InstarUserData> _userDataTable;
	private readonly Dictionary<DateTime, Notification> _notifications;

	// Although we don't mock anything here, we use this to
	// throw exceptions if they're configured.
	public Mock<IDatabaseService> Mock { get; } = new();

	public Mock<IDynamoDBContext> ContextMock { get; } = new();

	public TestDatabaseService(IDynamicConfigService dynamicConfig, TimeProvider timeProvider)
	{
		_dynamicConfig = dynamicConfig;
		_timeProvider = timeProvider;
		_userDataTable = new Dictionary<Snowflake, InstarUserData>();
		_notifications = [];

		SetupContextMock(_userDataTable, data => data.UserID!);
		SetupContextMock(_notifications, notif => notif.Date);
	}

	private void SetupContextMock<T, V>(Dictionary<V, T> mapPointer, Func<T, V> keySelector) where V : notnull
	{
		ContextMock.Setup(n => n.DeleteAsync(It.IsAny<T>())).Callback((T data, CancellationToken _) =>
		{
			var key = keySelector(data);
			mapPointer.Remove(key);
		});

		ContextMock.Setup(n => n.SaveAsync(It.IsAny<T>())).Callback((T data, CancellationToken _) =>
		{
			var key = keySelector(data);
			mapPointer[key] = data;
		});
	}

	public async Task<InstarDatabaseEntry<InstarUserData>?> GetUserAsync(Snowflake snowflake)
	{
		await Mock.Object.GetUserAsync(snowflake);

		return !_userDataTable.TryGetValue(snowflake, out var userData)
			? null
			: new InstarDatabaseEntry<InstarUserData>(ContextMock.Object, userData);
	}

	public Task<InstarDatabaseEntry<InstarUserData>> GetOrCreateUserAsync(IGuildUser user)
	{
		Mock.Object.GetOrCreateUserAsync(user);

		if (!_userDataTable.TryGetValue(user.Id, out var userData))
		{
			userData = InstarUserData.CreateFrom(user);
			_userDataTable[user.Id] = userData;
		}

		return Task.FromResult(new InstarDatabaseEntry<InstarUserData>(ContextMock.Object, userData));
	}

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration", Justification = "Doesn't actually enumerate multiple times. First 'enumeration' is a mock which does nothing.")]
	public Task<List<InstarDatabaseEntry<InstarUserData>>> GetBatchUsersAsync(IEnumerable<Snowflake> snowflakes)
	{
		Mock.Object.GetBatchUsersAsync(snowflakes);

		var returnList = new List<InstarUserData>();
		foreach (Snowflake snowflake in snowflakes)
		{
			if (_userDataTable.TryGetValue(snowflake, out var userData))
				returnList.Add(userData);
		}

		return Task.FromResult(returnList.Select(data => new InstarDatabaseEntry<InstarUserData>(ContextMock.Object, data)).ToList());
	}

	public Task CreateUserAsync(InstarUserData data)
	{
		Mock.Object.CreateUserAsync(data);

		_userDataTable[data.UserID!] = data;
		return Task.CompletedTask;
	}

	public Task<List<InstarDatabaseEntry<InstarUserData>>> GetUsersByBirthday(DateTimeOffset birthdate, TimeSpan fuzziness)
	{
		Mock.Object.GetUsersByBirthday(birthdate, fuzziness);

		var startUtc = birthdate.ToUniversalTime() - fuzziness;
		var endUtc = birthdate.ToUniversalTime() + fuzziness; 

		var matchedUsers = _userDataTable.Values.Where(userData =>
		{
			if (userData.Birthday == null)
				return false;
			var userBirthdateThisYear = userData.Birthday.Observed.ToUniversalTime();
			return userBirthdateThisYear >= startUtc && userBirthdateThisYear <= endUtc;
		}).ToList();

		return Task.FromResult(matchedUsers.Select(data => new InstarDatabaseEntry<InstarUserData>(ContextMock.Object, data)).ToList());
	}

	public async Task<List<InstarDatabaseEntry<Notification>>> GetPendingNotifications()
	{
		await Mock.Object.GetPendingNotifications();

		var currentTimeUtc = _timeProvider.GetUtcNow();

		var cfg = await _dynamicConfig.GetConfig();

		var pendingNotifications = _notifications.Values
			.Where(notification => notification.Date <= currentTimeUtc && notification.GuildID == cfg.TargetGuild)
			.ToList();

		return pendingNotifications.Select(data => new InstarDatabaseEntry<Notification>(ContextMock.Object, data)).ToList();
	}

	public Task<InstarDatabaseEntry<Notification>> CreateNotificationAsync(Notification notification)
	{
		Mock.Object.CreateNotificationAsync(notification);

		_notifications[notification.Date] = notification;
		return Task.FromResult(new InstarDatabaseEntry<Notification>(ContextMock.Object, notification));
	}

	public Task<List<InstarDatabaseEntry<Notification>>> GetNotificationsByTypeAndReferenceUser(NotificationType type, Snowflake userId)
	{
		return Task.FromResult(
				_notifications.Values
					.Where(n => n.Type == type && n.ReferenceUser == userId)
					.Select(n => new InstarDatabaseEntry<Notification>(ContextMock.Object, n))
					.ToList()
			);
	}

	public List<Notification> GetAllNotifications()
	{
		return _notifications.Values.ToList();
	}

	public void DeleteUser(Snowflake userId)
	{
		_userDataTable.Remove(userId);
	}
}