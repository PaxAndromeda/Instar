using InstarBot.Test.Framework.Models;
using JetBrains.Annotations;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Gaius;
using PaxAndromeda.Instar.Services;

namespace InstarBot.Test.Framework.Services;

[UsedImplicitly]
public sealed class TestGaiusAPIService (
	Dictionary<Snowflake, List<Warning>> warnings,
	Dictionary<Snowflake, List<Caselog>> caselogs,
	bool inhibit = false)
	: IGaiusAPIService
{
	private bool _inhibit = inhibit;

	public void Inhibit()
	{
		_inhibit = true;
	}

	public void AddWarning(TestGuildUser user, Warning warning)
	{
		if (!warnings.ContainsKey(user.Id))
			warnings[user.Id] = [];

		warnings[user.Id].Add(warning);
	}

	public void AddCaselog(TestGuildUser user, Caselog caselog)
	{
		if (!caselogs.ContainsKey(user.Id))
			caselogs[user.Id] = [ ];

		caselogs[user.Id].Add(caselog);
	}

	public TestGaiusAPIService() :
		this(false) { }

	public TestGaiusAPIService(bool inhibit)
		: this(new Dictionary<Snowflake, List<Warning>>(), new Dictionary<Snowflake, List<Caselog>>(), inhibit)
	{ }

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
        if (_inhibit)
            return Task.FromResult<IEnumerable<Warning>?>(null);
        
        return !warnings.TryGetValue(userId, out var warning)
            ? Task.FromResult<IEnumerable<Warning>?>([])
            : Task.FromResult<IEnumerable<Warning>?>(warning);
    }

    public Task<IEnumerable<Caselog>?> GetCaselogs(Snowflake userId)
    {
        if (_inhibit)
            return Task.FromResult<IEnumerable<Caselog>?>(null);
        
        return !caselogs.TryGetValue(userId, out var caselog)
            ? Task.FromResult<IEnumerable<Caselog>?>([])
            : Task.FromResult<IEnumerable<Caselog>?>(caselog);
    }
}