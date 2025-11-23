using Amazon.DynamoDBv2.DataModel;
using Discord;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Services;

namespace InstarBot.Tests.Services;

/// <summary>
/// A mock implementation of the <c>IInstarDDBService</c> interface for unit testing purposes.
/// This class provides an in-memory storage mechanism to simulate DynamoDB operations.
/// </summary>
public sealed class MockInstarDDBService : IInstarDDBService
{
    private readonly Dictionary<Snowflake, InstarUserData> _localData;

    public MockInstarDDBService()
    {
        _localData = new Dictionary<Snowflake, InstarUserData>();
    }

    public MockInstarDDBService(IEnumerable<InstarUserData> preload)
    {
        _localData = preload.ToDictionary(n => n.UserID!, n => n);
    }

    public void Register(InstarUserData data)
    {
        _localData.TryAdd(data.UserID!, data);
    }

    public Task<InstarDatabaseEntry<InstarUserData>?> GetUserAsync(Snowflake snowflake)
    {
        if (!_localData.TryGetValue(snowflake, out var data))
            throw new InvalidOperationException("User not found.");

        var ddbContextMock = new Mock<IDynamoDBContext>();

        return Task.FromResult(new InstarDatabaseEntry<InstarUserData>(ddbContextMock.Object, data))!;
    }

    public Task<InstarDatabaseEntry<InstarUserData>> GetOrCreateUserAsync(IGuildUser user)
    {
        if (!_localData.TryGetValue(user.Id, out var data))
            data = InstarUserData.CreateFrom(user);

        var ddbContextMock = new Mock<IDynamoDBContext>();

        return Task.FromResult(new InstarDatabaseEntry<InstarUserData>(ddbContextMock.Object, data));
    }

    public Task<List<InstarDatabaseEntry<InstarUserData>>> GetBatchUsersAsync(IEnumerable<Snowflake> snowflakes)
    {
        return Task.FromResult(GetLocalUsers(snowflakes)
            .Select(n => new InstarDatabaseEntry<InstarUserData>(new Mock<IDynamoDBContext>().Object, n)).ToList());
    }

    private IEnumerable<InstarUserData> GetLocalUsers(IEnumerable<Snowflake> snowflakes)
    {
        foreach (var snowflake in snowflakes)
            if (_localData.TryGetValue(snowflake, out var data))
                yield return data;
    }

    public Task CreateUserAsync(InstarUserData data)
    {
        _localData.TryAdd(data.UserID!, data);
        return Task.CompletedTask;
    }
}