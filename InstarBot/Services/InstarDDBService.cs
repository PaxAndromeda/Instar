using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Discord;
using Microsoft.Extensions.Configuration;
using PaxAndromeda.Instar.DynamoModels;
using Serilog;
using System.Diagnostics.CodeAnalysis;

namespace PaxAndromeda.Instar.Services;

[ExcludeFromCodeCoverage]
public sealed class InstarDDBService : IInstarDDBService
{
	private readonly TimeProvider _timeProvider;
	private readonly DynamoDBContext _ddbContext;
	private readonly string _guildId;

    public InstarDDBService(IConfiguration config, TimeProvider timeProvider)
    {
	    _timeProvider = timeProvider;
	    var region = config.GetSection("AWS").GetValue<string>("Region");

		_guildId = config.GetValue<string>("TargetGuild")
			?? throw new ConfigurationException("TargetGuild is not set.");

        var client = new AmazonDynamoDBClient(new AWSIAMCredential(config), RegionEndpoint.GetBySystemName(region));
        _ddbContext = new DynamoDBContextBuilder()
            .WithDynamoDBClient(() => client)
            .Build();
        
        _ddbContext.ConverterCache.Add(typeof(InstarUserDataHistoricalEntry<string>), new ArbitraryDynamoDBTypeConverter<InstarUserDataHistoricalEntry<string>>());
    }
    
    public async Task<InstarDatabaseEntry<InstarUserData>?> GetUserAsync(Snowflake snowflake)
    {
        try
        {
            var result = await _ddbContext.LoadAsync<InstarUserData>(_guildId, snowflake.ID.ToString());
            return result is null ? null : new InstarDatabaseEntry<InstarUserData>(_ddbContext, result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get user data for {Snowflake}", snowflake);
            return null;
        }
    }

    public async Task<InstarDatabaseEntry<InstarUserData>> GetOrCreateUserAsync(IGuildUser user)
    {
        var data = await _ddbContext.LoadAsync<InstarUserData>(_guildId, user.Id.ToString()) ?? InstarUserData.CreateFrom(user);

        return new InstarDatabaseEntry<InstarUserData>(_ddbContext, data);
    }

    public async Task<List<InstarDatabaseEntry<InstarUserData>>> GetBatchUsersAsync(IEnumerable<Snowflake> snowflakes)
    {
        var batches = _ddbContext.CreateBatchGet<InstarUserData>();
        foreach (var snowflake in snowflakes)
            batches.AddKey(_guildId, snowflake.ID.ToString());

        await _ddbContext.ExecuteBatchGetAsync(batches);

        return batches.Results.Select(x => new InstarDatabaseEntry<InstarUserData>(_ddbContext, x)).ToList();
    }

	public async Task<List<InstarDatabaseEntry<InstarUserData>>> GetUsersByBirthday(DateTimeOffset birthdate, TimeSpan fuzziness)
	{
		var birthday = new Birthday(birthdate, _timeProvider);
		var birthdayUtc = birthday.Birthdate.ToUniversalTime();


		var start = birthdayUtc - fuzziness;
		var end   = birthdayUtc + fuzziness;

		// Build one or two ranges depending on whether we cross midnight
		var ranges = new List<(string From, string To)>();

		if (start.Date == end.Date)
			ranges.Add((Utilities.ToBirthdateKey(start), Utilities.ToBirthdateKey(end)));
		else
		{
			// Range 1: start -> end of start day
			var endOfStartDay = new DateTime(start.Year, start.Month, start.Day, 23, 59, 59, DateTimeKind.Utc);
			ranges.Add((Utilities.ToBirthdateKey(start), Utilities.ToBirthdateKey(endOfStartDay)));

			// Range 2: start of end day -> end
			var startOfEndDay = new DateTime(end.Year, end.Month, end.Day, 0, 0, 0, DateTimeKind.Utc);
			ranges.Add((Utilities.ToBirthdateKey(startOfEndDay), Utilities.ToBirthdateKey(end)));
		}

		var results = new List<InstarDatabaseEntry<InstarUserData>>();

		foreach (var range in ranges)
		{
			var config = new QueryOperationConfig
			{
				IndexName = "birthdate-gsi",
				KeyExpression = new Expression
				{
					ExpressionStatement = "guild_id = :g AND birthdate BETWEEN :from AND :to",
					ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
					{
						[":g"]    = _guildId,
						[":from"] = range.From,
						[":to"]   = range.To
					}
				}
			};

			var search = _ddbContext.FromQueryAsync<InstarUserData>(config);

			var page = await search.GetRemainingAsync().ConfigureAwait(false);
			results.AddRange(page.Select(u => new InstarDatabaseEntry<InstarUserData>(_ddbContext, u)));
		}

		return results;
	}

    public async Task CreateUserAsync(InstarUserData data)
    {
        await _ddbContext.SaveAsync(data);
    }
}