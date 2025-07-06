using InstarBot.Tests.Models;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Services;

namespace InstarBot.Tests.Services;

public sealed class MockInstarDDBService : IInstarDDBService
{
    private readonly Dictionary<Snowflake, UserDatabaseInformation> _localData;

    public MockInstarDDBService()
    {
        _localData = new Dictionary<Snowflake, UserDatabaseInformation>();
    }

    public MockInstarDDBService(IEnumerable<UserDatabaseInformation> preload)
    {
        _localData = preload.ToDictionary(n => n.Snowflake, n => n);
    }

    public Task<bool> UpdateUserBirthday(Snowflake snowflake, DateTime birthday)
    {
        _localData.TryAdd(snowflake, new UserDatabaseInformation(snowflake));
        _localData[snowflake].Birthday = birthday;

        return Task.FromResult(true);
    }

    public Task<bool> UpdateUserJoinDate(Snowflake snowflake, DateTime joinDate)
    {
        _localData.TryAdd(snowflake, new UserDatabaseInformation(snowflake));
        _localData[snowflake].JoinDate = joinDate;

        return Task.FromResult(true);
    }

    public Task<bool> UpdateUserMembership(Snowflake snowflake, bool membershipGranted)
    {
        _localData.TryAdd(snowflake, new UserDatabaseInformation(snowflake));
        _localData[snowflake].GrantedMembership = membershipGranted;

        return Task.FromResult(true);
    }

    public Task<DateTimeOffset?> GetUserBirthday(Snowflake snowflake)
    {
        return !_localData.TryGetValue(snowflake, out var value)
            ? Task.FromResult<DateTimeOffset?>(null)
            : Task.FromResult<DateTimeOffset?>(value.Birthday);
    }

    public Task<DateTimeOffset?> GetUserJoinDate(Snowflake snowflake)
    {
        return !_localData.TryGetValue(snowflake, out var value)
            ? Task.FromResult<DateTimeOffset?>(null)
            : Task.FromResult<DateTimeOffset?>(value.JoinDate);
    }

    public Task<bool?> GetUserMembership(Snowflake snowflake)
    {
        return !_localData.TryGetValue(snowflake, out var value)
            ? Task.FromResult<bool?>(null)
            : Task.FromResult<bool?>(value.GrantedMembership);
    }
}