using JetBrains.Annotations;
using PaxAndromeda.Instar.Metrics;
using PaxAndromeda.Instar.Services;

namespace InstarBot.Tests.Services;

public sealed class MockMetricService : IMetricService
{
    private readonly List<(Metric, double)> _emittedMetrics = [];
    
    public Task<bool> Emit(Metric metric, double value)
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