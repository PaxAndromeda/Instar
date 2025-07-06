using System.Net;
using Amazon;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Microsoft.Extensions.Configuration;
using PaxAndromeda.Instar.Metrics;
using Serilog;
using Metric = PaxAndromeda.Instar.Metrics.Metric;

namespace PaxAndromeda.Instar.Services;

public sealed class CloudwatchMetricService : IMetricService
{
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
        try
        {
            var nameAttr = metric.GetAttributeOfType<MetricNameAttribute>();

            var datum = new MetricDatum
            {
                MetricName = nameAttr is not null ? nameAttr.Name : Enum.GetName(metric),
                Value = value
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
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to emit metric {Metric} with value {Value}", metric, value);
            return false;
        }
    }
}