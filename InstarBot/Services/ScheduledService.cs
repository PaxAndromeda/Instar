using NCrontab;
using PaxAndromeda.Instar.Metrics;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;
using System.Timers;
using Timer = System.Timers.Timer;

namespace PaxAndromeda.Instar.Services;

/// <summary>
/// An abstract base class for services that need to run on a scheduled basis using cron expressions.
/// </summary>
public abstract class ScheduledService : IStartableService, IRunnableService
{
	private const string ServiceDimension = "Service";

	private readonly string _serviceName;
	private readonly TimeProvider _timeProvider;
	private readonly IMetricService _metricService;
	private readonly CrontabSchedule _schedule;
	private readonly Enricher _enricher;
	private Timer? _nextRunTimer;
	private DateTime _expectedNextTime;

	/// <summary>
	/// Initializes a new instance of the ScheduledService class with the specified schedule, time provider, metric
	/// service, and optional service name.
	/// </summary>
	/// <param name="cronExpression">A cron expression that defines the schedule on which the service should run. Must be a valid cron format string.</param>
	/// <param name="timeProvider">A time provider used to determine the current time for scheduling operations.</param>
	/// <param name="metricService">A metric service used to record and report service metrics.</param>
	/// <param name="serviceName">An optional name for the service which is used for the "Service" dimension in emitted metrics. If not provided, the name of
	/// the derived class is used. If the name cannot be determined, defaults to "Unknown Service".</param>
	/// <exception cref="ArgumentException">Thrown if the provided cron expression is not valid.</exception>
	protected ScheduledService(string cronExpression, TimeProvider timeProvider, IMetricService metricService, string? serviceName = null)
	{
		// Hacky way to get the service name if one isn't provided
		try
		{
			_serviceName = serviceName
				?? new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name // The name of the derived class
				?? "Unknown Service"; // Fallback if all else fails
		} catch
		{
			// Swallow any exceptions and use the fallback
			_serviceName = "Unknown Service";
		}

		_timeProvider = timeProvider;
		_metricService = metricService;
		_enricher = new Enricher(_serviceName);
		
		try
		{
			_schedule = CrontabSchedule.Parse(cronExpression);
		} catch (Exception ex)
		{
			throw new ArgumentException("Provided cron expression was not valid.", nameof(cronExpression), ex);
		}
	}

	public async Task Start()
	{
		_nextRunTimer = new Timer
		{
			AutoReset = false,
			Enabled = false
		};

		_nextRunTimer.Elapsed += TimerElapsed;
		
		await Initialize();

		ScheduleNext();
	}

	private void ScheduleNext()
	{
		if (_nextRunTimer is null)
			throw new InvalidOperationException("Service has not been started. Call Start() before scheduling the next run.");

		var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
		_expectedNextTime = _schedule.GetNextOccurrence(utcNow);

		Log.ForContext(_enricher).Debug("[{Service}] Next scheduled start time: {NextTime}", _serviceName, _expectedNextTime);

		var timeToNextRun = _expectedNextTime - utcNow;

		_nextRunTimer.Interval = timeToNextRun.TotalMilliseconds;
		_nextRunTimer.Start();
	}

	// ReSharper disable once AsyncVoidEventHandlerMethod
	private async void TimerElapsed(object? _, ElapsedEventArgs elapsedEventArgs)
	{
		try
		{
			var deviation = elapsedEventArgs.SignalTime.ToUniversalTime() - _expectedNextTime;

			Log.ForContext(_enricher).Debug("[{Service}] Timer elapsed. Deviation is {Deviation}ms", _serviceName, deviation.TotalMilliseconds);

			await _metricService.Emit(Metric.ScheduledService_ScheduleDeviation, deviation.TotalMilliseconds, new Dictionary<string, string>
			{
				[ServiceDimension] = _serviceName
			});
		} catch (Exception ex)
		{
			Log.ForContext(_enricher).Error(ex, "Failed to emit timer deviation metric");
		}

		try
		{
			Stopwatch stopwatch = new();
			
			stopwatch.Start();
			await RunAsync();
			stopwatch.Stop();

			Log.ForContext(_enricher).Debug("[{Service}] Run completed. Total run time was {TotalRuntimeMs}ms", _serviceName, stopwatch.ElapsedMilliseconds);
			await _metricService.Emit(Metric.ScheduledService_ServiceRuntime, stopwatch.ElapsedMilliseconds, new Dictionary<string, string>
			{
				[ServiceDimension] = _serviceName
			});
		}
		catch (Exception ex)
		{
			Log.ForContext(_enricher).Error(ex, "Failed to execute scheduled task.");
		} finally
		{
			ScheduleNext();
		}
	}

	/// <summary>
	/// Initialize the scheduled service.
	/// </summary>
	internal abstract Task Initialize();

	/// <summary>
	/// Executes the scheduled service at the scheduled time.
	/// </summary>
	public abstract Task RunAsync();

	private class Enricher(string serviceName) : ILogEventEnricher
	{
		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			// Create a log event property for "ProjectSlug".
			LogEventProperty logEventProperty = propertyFactory.CreateProperty(
				"Service",
				serviceName
			);

			// Add the property to the log event if it is not already present.
			logEvent.AddPropertyIfAbsent(logEventProperty);
		}
	}
}