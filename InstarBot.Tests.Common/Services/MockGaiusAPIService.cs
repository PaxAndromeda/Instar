using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Gaius;
using PaxAndromeda.Instar.Services;

namespace InstarBot.Tests.Services;

public sealed class MockGaiusAPIService(
    Dictionary<Snowflake, List<Warning>> warnings,
    Dictionary<Snowflake, List<Caselog>> caselogs,
    bool inhibit = false)
    : IGaiusAPIService
{
    public void Dispose()
    {
        // do nothing
    }

    public Task<IEnumerable<Warning>> GetAllWarnings()
    {
        return Task.FromResult<IEnumerable<Warning>>(warnings.Values.SelectMany(list => list).ToList());
    }

    public Task<IEnumerable<Caselog>> GetAllCaselogs()
    {
        return Task.FromResult<IEnumerable<Caselog>>(caselogs.Values.SelectMany(list => list).ToList());
    }

    public Task<IEnumerable<Warning>> GetWarningsAfter(DateTime dt)
    {
        return Task.FromResult<IEnumerable<Warning>>(from list in warnings.Values from item in list where item.WarnDate > dt select item);
    }

    public Task<IEnumerable<Caselog>> GetCaselogsAfter(DateTime dt)
    {
        return Task.FromResult<IEnumerable<Caselog>>(from list in caselogs.Values from item in list where item.Date > dt select item);
    }

    public Task<IEnumerable<Warning>?> GetWarnings(Snowflake userId)
    {
        if (inhibit)
            return Task.FromResult<IEnumerable<Warning>?>(null);
        
        return !warnings.TryGetValue(userId, out var warning)
            ? Task.FromResult<IEnumerable<Warning>?>([])
            : Task.FromResult<IEnumerable<Warning>?>(warning);
    }

    public Task<IEnumerable<Caselog>?> GetCaselogs(Snowflake userId)
    {
        if (inhibit)
            return Task.FromResult<IEnumerable<Caselog>?>(null);
        
        return !caselogs.TryGetValue(userId, out var caselog)
            ? Task.FromResult<IEnumerable<Caselog>?>([])
            : Task.FromResult<IEnumerable<Caselog>?>(caselog);
    }
}