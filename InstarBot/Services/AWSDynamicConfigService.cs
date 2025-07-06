using System.Net;
using System.Text;
using Amazon.AppConfigData;
using Amazon;
using Amazon.AppConfigData.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PaxAndromeda.Instar.ConfigModels;
using Serilog;

namespace PaxAndromeda.Instar.Services;

public interface IDynamicConfigService
{
    Task<InstarDynamicConfiguration> GetConfig();
    Task<string?> GetParameter(string parameterName);
    Task Initialize();
}

public sealed class AWSDynamicConfigService : IDynamicConfigService
{
    private readonly AmazonAppConfigDataClient _appConfigDataClient;
    private readonly AmazonSimpleSystemsManagementClient _ssmClient;
    private string _configData = null!;
    private string _nextToken = null!;
    private DateTime _nextPollTime;
    private InstarDynamicConfiguration _current = null!;
    private readonly SemaphoreSlim _pollSemaphore = new(1, 1);

    private readonly string _application;
    private readonly string _environment;
    private readonly string _configProfile;

    public AWSDynamicConfigService(IConfiguration config)
    {
        Guard.Against.Null(config);

        var awsSection = config.GetSection("AWS");
        var appConfigSection = awsSection.GetSection("AppConfig");

        var region = awsSection.GetValue<string>("Region");
        var iam = new AWSIAMCredential(config);

        _application = appConfigSection.GetValue<string>("Application") ??
                       throw new ConfigurationException("Invalid AppConfig/Application configuration");
        _environment = appConfigSection.GetValue<string>("Environment") ??
                       throw new ConfigurationException("Invalid AppConfig/Environment configuration");
        _configProfile = appConfigSection.GetValue<string>("ConfigurationProfile") ??
                         throw new ConfigurationException("Invalid AppConfig/ConfigurationProfile configuration");


        Log.Information("AppConfig Application={Application}, Environment={Environment}, ConfigProfile={ConfigProfile}",
            _application, _environment, _configProfile);

        _appConfigDataClient = new AmazonAppConfigDataClient(iam, RegionEndpoint.GetBySystemName(region));
        _ssmClient = new AmazonSimpleSystemsManagementClient(iam, RegionEndpoint.GetBySystemName(region));
    }

    public async Task<InstarDynamicConfiguration> GetConfig()
    {
        try
        {
            await _pollSemaphore.WaitAsync();
            if (DateTime.UtcNow > _nextPollTime)
                await Poll(false);

            return _current;
        }
        finally
        {
            _pollSemaphore.Release();
        }
    }

    public async Task<string?> GetParameter(string parameterName)
    {
        var config = await GetConfig();
        
        var response = await _ssmClient.GetParameterAsync(new GetParameterRequest
        {
            Name = $"{config.AWS.AppConfig.ParameterNamespace}/{parameterName}",
            WithDecryption = true
        });

        return response.HttpStatusCode != HttpStatusCode.OK ? null : response.Parameter.Value;
    }

    public async Task Initialize()
    {
        var configSession = await _appConfigDataClient.StartConfigurationSessionAsync(new StartConfigurationSessionRequest
        {
            ApplicationIdentifier = _application,
            EnvironmentIdentifier = _environment,
            ConfigurationProfileIdentifier = _configProfile
        });

        _nextToken = configSession.InitialConfigurationToken;
        await Poll(true);
    }

    private async Task Poll(bool bypass)
    {
        try
        {
            var result = await _appConfigDataClient.GetLatestConfigurationAsync(new GetLatestConfigurationRequest
            {
                ConfigurationToken = _nextToken
            });

            _nextToken = result.NextPollConfigurationToken;
            _nextPollTime = DateTime.UtcNow + TimeSpan.FromSeconds((double)(result.NextPollIntervalInSeconds ?? 60));
            
            // Per the documentation, if VersionLabel is empty, then the client
            // has the most up-to-date configuration already stored.  We can stop
            // here.
            if (!bypass && string.IsNullOrEmpty(result.VersionLabel))
                return;

            if (!string.IsNullOrEmpty(result.VersionLabel))
                Log.Information("Downloading latest configuration version {ConfigVersion} from AppConfig...", result.VersionLabel);
            else
                Log.Information("Downloading latest configuration from AppConfig...");

            _configData = Encoding.UTF8.GetString(result.Configuration.ToArray());
            _current = JsonConvert.DeserializeObject<InstarDynamicConfiguration>(_configData) ??
                       throw new ConfigurationException("Failed to parse configuration");
            
            Log.Information("Done downloading latest configuration!");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to poll configuration");
        }
    }
}