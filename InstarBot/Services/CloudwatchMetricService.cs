using System.Net;
using Amazon;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using PaxAndromeda.Instar.Metrics;
using Serilog;
using Metric = PaxAndromeda.Instar.Metrics.Metric;

namespace PaxAndromeda.Instar.Services;

public sealed class CloudwatchMetricService : IMetricService
{
	// Exponential backoff parameters
	private const int MaxAttempts = 5;
	private static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds(200);

	private readonly AmazonCloudWatchClient _client;
    private readonly string _metricNamespace;
    public CloudwatchMetricService(IConfiguration config)
    {
        var region = config.GetSection("AWS").GetValue<string>("Region");
        _metricNamespace = config.GetSection("AWS").GetSection("CloudWatch").GetValue<string>("MetricNamespace")!;

        _client = new AmazonCloudWatchClient(new AWSIAMCredential(config), RegionEndpoint.GetBySystemName(region));
    }

    public async Task<bool> Emit(Metric metric, double value)
    {
		for (var attempt = 1; attempt <= MaxAttempts; attempt++)
		{
			try
			{
				var nameAttr = metric.GetAttributeOfType<MetricNameAttribute>();

				var datum = new MetricDatum
				{
					MetricName = nameAttr is not null ? nameAttr.Name : Enum.GetName(metric),
					Value = value,
					Dimensions = []
				};

				var attrs = metric.GetAttributesOfType<MetricDimensionAttribute>();
				if (attrs != null)
					foreach (var dim in attrs)
					{
						datum.Dimensions.Add(new Dimension
						{
							Name = dim.Name,
							Value = dim.Value
						});
					}

				var response = await _client.PutMetricDataAsync(new PutMetricDataRequest
				{
					Namespace = _metricNamespace,
					MetricData = [datum]
				});

				return response.HttpStatusCode == HttpStatusCode.OK;
			} catch (Exception ex) when (IsTransient(ex) && attempt < MaxAttempts)
			{
				var expo = Math.Pow(2, attempt - 1);
				var jitter = TimeSpan.FromMilliseconds(new Random().NextDouble() * 100);
				var delay = TimeSpan.FromMilliseconds(BaseDelay.TotalMilliseconds * expo) + jitter;

				await Task.Delay(delay);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to emit metric {Metric} with value {Value}", metric, value);
				return false;
			}
		}

		// If we exit the loop without returning, it failed after retries.
		Log.Error("Exceeded retry attempts emitting metric {Metric} with value {Value}", metric, value);
		return false;
	}

	private static bool IsTransient(Exception ex)
	{
		return ex switch
		{
			AmazonServiceException ase =>
				// 5xx errors / throttles are transient
				ase.StatusCode == HttpStatusCode.InternalServerError || (int) ase.StatusCode >= 500 ||
				ase.ErrorCode.Contains("Throttling", StringComparison.OrdinalIgnoreCase) ||
				ase.ErrorCode.Contains("Throttled", StringComparison.OrdinalIgnoreCase),
			// clientside issues
			AmazonClientException or WebException => true,
			// gracefully handle cancels
			OperationCanceledException or TaskCanceledException => false,
			_ => ex is TimeoutException
		};
	}
}