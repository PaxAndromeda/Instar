using System.Diagnostics.CodeAnalysis;
using Amazon.DynamoDBv2.DataModel;
using Discord;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Services;

namespace InstarBot.Test.Framework.Services;

public class TestDatabaseService : IMockOf<IDatabaseService>, IDatabaseService
{
	private readonly TimeProvider _timeProvider;
	private readonly Dictionary<Snowflake, InstarUserData> _userDataTable;
	private readonly Dictionary<DateTime, Notification> _notifications;
	private readonly Mock<IDynamoDBContext> _contextMock;

	// Although we don't mock anything here, we use this to
	// throw exceptions if they're configured.
	public Mock<IDatabaseService> Mock { get; } = new();

	public TestDatabaseService(TimeProvider timeProvider)
	{
		_timeProvider = timeProvider;
		_userDataTable = new Dictionary<Snowflake, InstarUserData>();
		_notifications = [];
		_contextMock = new Mock<IDynamoDBContext>();

		SetupContextMock(_userDataTable, data => data.UserID!);
		SetupContextMock(_notifications, notif => notif.Date);
	}

	private void SetupContextMock<T, V>(Dictionary<V, T> mapPointer, Func<T, V> keySelector)
	{
		_contextMock.Setup(n => n.DeleteAsync(It.IsAny<T>())).Callback((T data, CancellationToken _) =>
		{
			var key = keySelector(data);
			mapPointer.Remove(key);
		});

		_contextMock.Setup(n => n.SaveAsync(It.IsAny<T>())).Callback((T data, CancellationToken _) =>
		{
			var key = keySelector(data);
			mapPointer[key] = data;
		});
	}

	public Task<InstarDatabaseEntry<InstarUserData>?> GetUserAsync(Snowflake snowflake)
	{
		Mock.Object.GetUserAsync(snowflake);

		return !_userDataTable.TryGetValue(snowflake, out var userData)
			? Task.FromResult<InstarDatabaseEntry<InstarUserData>>(null)
			: Task.FromResult(new InstarDatabaseEntry<InstarUserData>(_contextMock.Object, userData));
	}

	public Task<InstarDatabaseEntry<InstarUserData>> GetOrCreateUserAsync(IGuildUser user)
	{
		Mock.Object.GetOrCreateUserAsync(user);

		if (!_userDataTable.TryGetValue(user.Id, out var userData))
		{
			userData = InstarUserData.CreateFrom(user);
			_userDataTable[user.Id] = userData;
		}

		return Task.FromResult(new InstarDatabaseEntry<InstarUserData>(_contextMock.Object, userData));
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

		return Task.FromResult(returnList.Select(data => new InstarDatabaseEntry<InstarUserData>(_contextMock.Object, data)).ToList());
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

		return Task.FromResult(matchedUsers.Select(data => new InstarDatabaseEntry<InstarUserData>(_contextMock.Object, data)).ToList());
	}

	public Task<List<InstarDatabaseEntry<Notification>>> GetPendingNotifications()
	{
		Mock.Object.GetPendingNotifications();

		var currentTimeUtc = _timeProvider.GetUtcNow();

		var pendingNotifications = _notifications.Values
			.Where(notification => notification.Date <= currentTimeUtc)
			.ToList();

		return Task.FromResult(pendingNotifications.Select(data => new InstarDatabaseEntry<Notification>(_contextMock.Object, data)).ToList());
	}

	public Task<InstarDatabaseEntry<Notification>> CreateNotificationAsync(Notification notification)
	{
		Mock.Object.CreateNotificationAsync(notification);

		_notifications[notification.Date] = notification;
		return Task.FromResult(new InstarDatabaseEntry<Notification>(_contextMock.Object, notification));
	}
}