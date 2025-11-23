using System.Diagnostics.CodeAnalysis;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Discord;
using Microsoft.Extensions.Configuration;
using PaxAndromeda.Instar.DynamoModels;
using Serilog;

namespace PaxAndromeda.Instar.Services;

[ExcludeFromCodeCoverage]
public sealed class InstarDDBService : IInstarDDBService
{
    private readonly DynamoDBContext _ddbContext;

    public InstarDDBService(IConfiguration config)
    {
        var region = config.GetSection("AWS").GetValue<string>("Region");

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
            var result = await _ddbContext.LoadAsync<InstarUserData>(snowflake.ID.ToString());
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
        var data = await _ddbContext.LoadAsync<InstarUserData>(user.Id.ToString()) ?? InstarUserData.CreateFrom(user);

        return new InstarDatabaseEntry<InstarUserData>(_ddbContext, data);
    }

    public async Task<List<InstarDatabaseEntry<InstarUserData>>> GetBatchUsersAsync(IEnumerable<Snowflake> snowflakes)
    {
        var batches = _ddbContext.CreateBatchGet<InstarUserData>();
        foreach (var snowflake in snowflakes)
            batches.AddKey(snowflake.ID.ToString());

        await _ddbContext.ExecuteBatchGetAsync(batches);

        return batches.Results.Select(x => new InstarDatabaseEntry<InstarUserData>(_ddbContext, x)).ToList();
    }

    public async Task CreateUserAsync(InstarUserData data)
    {
        await _ddbContext.SaveAsync(data);
    }
}