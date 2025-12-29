using JetBrains.Annotations;
using PaxAndromeda.Instar.Metrics;
using PaxAndromeda.Instar.Services;

namespace InstarBot.Test.Framework.Services;

public sealed class TestMetricService : IMetricService
{
    private readonly List<(Metric, double)> _emittedMetrics = [];
    
    public Task<bool> Emit(Metric metric, double value)
    {
        _emittedMetrics.Add((metric, value));
        return Task.FromResult(true);
    }

    public Task<bool> Emit(Metric metric, double value, Dictionary<string, string> dimensions)
	{
		_emittedMetrics.Add((metric, value));
		return Task.FromResult(true);
	}

    [UsedImplicitly]
    public IEnumerable<double> GetMetricValues(Metric metric)
    {
        foreach (var (em, val) in _emittedMetrics)
        {
            if (em != metric)
                continue;

            yield return val;
        }
    }
}