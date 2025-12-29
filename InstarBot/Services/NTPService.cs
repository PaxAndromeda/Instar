using GuerrillaNtp;
using PaxAndromeda.Instar.Metrics;
using Serilog;

namespace PaxAndromeda.Instar.Services;

/// <summary>
/// This service monitors and emits the clock drift metric which helps
/// operators be aware of a clock issue.
/// </summary>
/// <remarks>
/// Why? Many of our interactions have a time limit of 3 seconds for a
/// response. This is enforced locally with Discord.NET. It has been
/// observed that if there is a significant clock drift from UTC time,
/// the bot can enter an undefined state where it cannot process
/// interaction commands any further.
///
/// Whilst this is unlikely to occur on a dedicated blade server from
/// a hosting provider, having a metric for this to root cause issues
/// is crucial for investigatory purposes.
/// </remarks>
public class NTPService (TimeProvider timeProvider, IMetricService metricService)
	: ScheduledService("*/5 * * * *", timeProvider, metricService, "NTP Service")
{
	/// <summary>
	/// The hostname for NIST's NTP servers. We will trust them as the
	/// authority on time.
	/// </summary>
	private const string Hostname = "time.nist.gov";

	private readonly IMetricService _metricService = metricService;
	private NtpClient _ntpClient = null!;

	internal override Task Initialize()
	{
		_ntpClient = new NtpClient(Hostname);

		return Task.CompletedTask;
	}

	public override async Task RunAsync()
	{
		try
		{
			var result = await _ntpClient.QueryAsync();

			if (result is null)
			{
				Log.Error("Failed to query NTP time from {Hostname}.", Hostname);
				await _metricService.Emit(Metric.NTP_Error, 1);
				return;
			}

			await _metricService.Emit(Metric.NTP_Drift, result.CorrectionOffset.TotalMicroseconds);

			Log.Debug("[{ServiceName}] Time Drift: {TimeDrift:N}µs", "NTP", result.CorrectionOffset.TotalMicroseconds);

		} catch (Exception ex)
		{
			Log.Error(ex, "Failed to query NTP time from {Hostname}.", Hostname);

			try
			{
				await _metricService.Emit(Metric.NTP_Error, 1);
			} catch (Exception ex2)
			{
				Log.Error(ex2, "Failed to emit NTP query error metric.");
			}
		}
	}
}