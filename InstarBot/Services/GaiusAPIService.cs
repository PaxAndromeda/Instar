using Newtonsoft.Json;
using PaxAndromeda.Instar.Gaius;
using System.Text;

namespace PaxAndromeda.Instar.Services;

public sealed class GaiusAPIService(IDynamicConfigService config) : IGaiusAPIService
{
    // Used in release mode
    // ReSharper disable once NotAccessedField.Local
    private const string BaseURL = "https://api.gaiusbot.me";
    private const string WarningsBaseURL = BaseURL + "/warnings";
    private const string CaselogsBaseURL = BaseURL + "/caselogs";
    
    private readonly HttpClient _client = new();
    private string _apiKey = null!;
    private bool _initialized;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private async Task Initialize()
    {
        if (_initialized)
            return;
        
        await _semaphore.WaitAsync();
        try
        {
            if (_initialized)
                return;

            _apiKey = await config.GetParameter("GaiusKey") ??
                      throw new ConfigurationException("Could not acquire Gaius API key");
            await VerifyKey();
            _initialized = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private async Task VerifyKey()
    {
        var cfg = await config.GetConfig();
        
        var targetGuild = cfg.TargetGuild;
        var keyData = Encoding.UTF8.GetString(Convert.FromBase64String(_apiKey));
        if (!ulong.TryParse(keyData[..keyData.IndexOf(':')], out var keyGuild))
            throw new ConfigurationException("Gaius API key is not in the correct format.");
        if (keyGuild != targetGuild.ID)
            throw new ConfigurationException("Configured Gaius API key is not for this guild.")
            {
                Data =
                {
                    { "TargetGuild", targetGuild },
                    { "KeyGuild", keyGuild }
                }
            };
    }
    
    public async Task<IEnumerable<Warning>> GetAllWarnings()
    {
        await Initialize();
        
        var response = await Get($"{WarningsBaseURL}/all");

        var result = JsonConvert.DeserializeObject<Warning[]>(response);
        return result ?? [];
    }
    
    public async Task<IEnumerable<Caselog>> GetAllCaselogs()
    {
        await Initialize();

        var response = await Get($"{CaselogsBaseURL}/all");
        return ParseCaselogs(response);
    }

    public async Task<IEnumerable<Warning>> GetWarningsAfter(DateTime dt)
    {
        await Initialize();

        var response = await Get($"{WarningsBaseURL}/after/{dt:O}");

        var result = JsonConvert.DeserializeObject<Warning[]>(response);
        return result ?? [];
    }

    public async Task<IEnumerable<Caselog>> GetCaselogsAfter(DateTime dt)
    {
        await Initialize();

        var response = await Get($"{CaselogsBaseURL}/after/{dt:O}");
        return ParseCaselogs(response);
    }

    public async Task<IEnumerable<Warning>?> GetWarnings(Snowflake userId)
    {
        await Initialize();

        var result = await Get($"{WarningsBaseURL}/{userId.ID}");
        return JsonConvert.DeserializeObject<Warning[]>(result);
    }

    public async Task<IEnumerable<Caselog>?> GetCaselogs(Snowflake userId)
    {
        await Initialize();

        var result = await Get($"{CaselogsBaseURL}/{userId.ID}");
        return ParseCaselogs(result);
    }

    private static IEnumerable<Caselog> ParseCaselogs(string response)
    {
        // Remove any instances of "totalCases"
        while (response.Contains("\"totalcases\":", StringComparison.OrdinalIgnoreCase))
        {
            var start = response.IndexOf("\"totalcases\":", StringComparison.OrdinalIgnoreCase); 
            var end = response.IndexOfAny([',', '}'], start);

        if (response.Length <= 2)
            yield break;

        var result = JsonConvert.DeserializeObject<Dictionary<int, Caselog>>(response)?.Values.ToArray();
        if (result is null)
            yield break;

        foreach (var caselog in result)
            yield return caselog;
    }

    private async Task<string> Get(string url)
    {
        var hrm = CreateRequest(url);
        var response = await _client.SendAsync(hrm);
        
        return await response.Content.ReadAsStringAsync();
    }

    private HttpRequestMessage CreateRequest(string url)
    {
        var hrm = new HttpRequestMessage();
        hrm.RequestUri = new Uri(url);
        hrm.Headers.Add("Accept", "application/json");
        hrm.Headers.Add("api-key", _apiKey);

        return hrm;
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}