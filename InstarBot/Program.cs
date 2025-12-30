using Amazon;
using Amazon.CloudWatchLogs;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.Services;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.AwsCloudWatch;
using System.Diagnostics.CodeAnalysis;

namespace PaxAndromeda.Instar;

[ExcludeFromCodeCoverage]
internal static class Program
{
    private static CancellationTokenSource _cts = null!;
    private static IServiceProvider _services = null!;

    // ReSharper disable once UnusedParameter.Global
    public static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;


#if DEBUG
        var configPath = "Config/Instar.debug.conf.json";
#else
        var configPath = "Config/Instar.conf.json";
#endif

		var cli = Parser.Default.ParseArguments<CommandLineOptions>(args).Value;
		if (!string.IsNullOrEmpty(cli.ConfigPath))
			configPath = cli.ConfigPath;

		Log.Information("Config path is {Path}", configPath);
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile(configPath)
            .Build();
        
        InitializeLogger(config, cli.LogLevel);
        
        Console.CancelKeyPress += StopSystem;
        await RunAsync(config);

        while (!_cts.IsCancellationRequested) await Task.Delay(100);
    }

    private static async void StopSystem(object? sender, ConsoleCancelEventArgs e)
    {
        try
		{
			await _services.GetRequiredService<IDiscordService>().Stop();
			await _cts.CancelAsync();
		}
        catch (Exception err)
        {
            Log.Fatal(err, "FATAL: Unhandled exception caught during shutdown");
        }
    }

    private static async Task RunAsync(IConfiguration config)
    {
        _cts = new CancellationTokenSource();
        _services = ConfigureServices(config);

        // First, we need to ensure that our dynamic config is loaded and available
        var dynamicConfig = _services.GetRequiredService<IDynamicConfigService>();
        await dynamicConfig.Initialize();

		var discordService = _services.GetRequiredService<IDiscordService>();
        await discordService.Start(_services);

		// Start up other systems
		List<Task> tasks = [
			_services.GetRequiredService<IBirthdaySystem>().Start(),
			_services.GetRequiredService<IAutoMemberSystem>().Start(),
			_services.GetRequiredService<INotificationService>().Start(),
			_services.GetRequiredService<NTPService>().Start()
		];

		Task.WaitAll(tasks);
	}

    private static void InitializeLogger(IConfiguration config, LogEventLevel? requestedLogLevel)
    {
		var logCfg = new LoggerConfiguration()
			.Enrich.FromLogContext()
			.MinimumLevel.Is(requestedLogLevel ?? LogEventLevel.Information)
            .WriteTo.Console();


		var awsSection = config.GetSection("AWS");
        var cwSection = awsSection.GetSection("CloudWatch");
        if (cwSection.GetValue<bool>("Enabled"))
        {
            var region = awsSection.GetValue<string>("Region");

            var cwClient = new AmazonCloudWatchLogsClient(new AWSIAMCredential(config), RegionEndpoint.GetBySystemName(region));
            
            logCfg = logCfg.WriteTo.AmazonCloudWatch(new CloudWatchSinkOptions
            {
                LogGroupName = cwSection.GetValue<string>("LogGroup"),
                TextFormatter = new JsonFormatter(renderMessage: true),
                MinimumLogEventLevel = requestedLogLevel ?? LogEventLevel.Information
			}, cwClient);
        }

        Log.Logger = logCfg.CreateLogger();
    }

    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.ExceptionObject as Exception, "FATAL: Unhandled exception caught");
    }

    private static ServiceProvider ConfigureServices(IConfiguration config)
    {
        var services = new ServiceCollection();

        // Global context items
        services.AddSingleton(config);
        
        // Services
        services.AddSingleton<TeamService>();
        services.AddTransient<IDatabaseService, InstarDynamoDBService>();
        services.AddTransient<IGaiusAPIService, GaiusAPIService>();
        services.AddSingleton<IDiscordService, DiscordService>();
		services.AddSingleton<IAutoMemberSystem, AutoMemberSystem>();
		services.AddSingleton<IBirthdaySystem, BirthdaySystem>();
		services.AddSingleton<IDynamicConfigService, AWSDynamicConfigService>();
		services.AddSingleton<INotificationService, NotificationService>();
		services.AddSingleton(TimeProvider.System);
		services.AddSingleton<NTPService>();

#if DEBUG
		services.AddSingleton<IMetricService, FileSystemMetricService>();
#else
        services.AddTransient<IMetricService, CloudwatchMetricService>();
#endif

		// Commands & Interactions
		services.AddTransient<PingCommand>();
        services.AddTransient<SetBirthdayCommand>();
        services.AddSingleton<PageCommand>();
        services.AddTransient<IContextCommand, ReportUserCommand>();
		services.AddTransient<AutoMemberHoldCommand>();

        return services.BuildServiceProvider();
    }
}