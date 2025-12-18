using System.Reflection;
using PaxAndromeda.Instar.Metrics;
using Serilog;

namespace PaxAndromeda.Instar.Services;

public class FileSystemMetricService : IMetricService
{
	public FileSystemMetricService()
	{
		Initialize();
	}

	public void Initialize()
	{
		// Initialize the metrics subdirectory
		// .GetEntryAssembly() can be null in some unmanaged contexts, but that
		// doesn't apply here.
		var currentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;

		Directory.CreateDirectory(Path.Combine(currentDirectory, "metrics"));
	}

	public Task<bool> Emit(Metric metric, double value)
	{
		return Emit(metric, value, new Dictionary<string, string>());
	}

	public Task<bool> Emit(Metric metric, double value, Dictionary<string, string> dimensions)
	{
		Log.Debug("[METRIC] {MetricName} Value: {Value}", Enum.GetName(metric), value);

		return Task.FromResult(true);
	}
}