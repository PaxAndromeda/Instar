using JetBrains.Annotations;
using Newtonsoft.Json;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.Services;

namespace InstarBot.Test.Framework.Services;

public sealed class TestDynamicConfigService : IDynamicConfigService
{
	private readonly string _configPath;
	private readonly Dictionary<string, string> _parameters = new();
	private InstarDynamicConfiguration _config = null!;

	public TestDynamicConfigService(string configPath)
	{
		_configPath = configPath;

		Task.Run(Initialize).Wait();
	}

	[UsedImplicitly]
	public TestDynamicConfigService(string configPath, Dictionary<string, string> parameters)
		: this(configPath)
	{
		_parameters = parameters;
	}

	public Task<InstarDynamicConfiguration> GetConfig()
	{
		return Task.FromResult(_config);
	}

	public Task<string?> GetParameter(string parameterName)
	{
		return Task.FromResult(_parameters[parameterName])!;
	}

	public async Task Initialize()
	{
		var data = await File.ReadAllTextAsync(_configPath);
		_config = JsonConvert.DeserializeObject<InstarDynamicConfiguration>(data)!;
	}
}