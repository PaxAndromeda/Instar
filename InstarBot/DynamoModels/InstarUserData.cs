using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Discord;
using JetBrains.Annotations;


// Non-nullable field must contain a non-null value when exiting constructor.
// Since this is a DTO type, we can safely ignore this warning.
#pragma warning disable CS8618

namespace PaxAndromeda.Instar.DynamoModels;

[DynamoDBTable("InstarUsers")]
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public class InstarUserData
{
	[DynamoDBHashKey("guild_id", Converter = typeof(InstarSnowflakePropertyConverter))]
	[DynamoDBGlobalSecondaryIndexHashKey("birthdate-gsi", AttributeName = "guild_id")]
	public Snowflake? GuildID { get; set; }

	[DynamoDBRangeKey("user_id", Converter = typeof(InstarSnowflakePropertyConverter))]
    public Snowflake? UserID { get; set; }

	[DynamoDBProperty("birthday", Converter = typeof(InstarBirthdatePropertyConverter))]
	public Birthday? Birthday { get; set; }

	[DynamoDBGlobalSecondaryIndexRangeKey("birthdate-gsi", AttributeName = "birthdate")]
	[DynamoDBProperty("birthdate")]
	public string? Birthdate { get; set; }

	[DynamoDBProperty("joined")]
    public DateTime? Joined { get; set; }
        
    [DynamoDBProperty("position", Converter = typeof(InstarEnumPropertyConverter<InstarUserPosition>))]
    public InstarUserPosition? Position { get; set; }
        
    [DynamoDBProperty("avatars")]
    public List<InstarUserDataHistoricalEntry<string>>? Avatars { get; set; }
        
    [DynamoDBProperty("nicknames")]
    public List<InstarUserDataHistoricalEntry<string>>? Nicknames { get; set; }
    
    [DynamoDBProperty("usernames")]
    public List<InstarUserDataHistoricalEntry<string>>? Usernames { get; set; }
    
    [DynamoDBProperty("modlog")]
    public List<InstarModLogEntry> ModLogs { get; set; }
    
    [DynamoDBProperty("reports")]
    public List<InstarUserDataReports> Reports { get; set; }
    
    [DynamoDBProperty("notes")]
    public List<InstarUserDataNote> Notes { get; set; }

	[DynamoDBProperty("amh")]
	public AutoMemberHoldRecord? AutoMemberHoldRecord { get; set; }

	public string Username
    {
        get => Usernames?.LastOrDefault()?.Data ?? "<unknown>";
        set
        {
			// We can't pass along a TimeProvider, so we'll
			// need to keep DateTime.UtcNow here.
			var time = DateTime.UtcNow;
            if (Usernames is null)
            {
                Usernames = [new InstarUserDataHistoricalEntry<string>(time, value)];
                return;
            }

			// Don't add a new username if the latest one matches the current one
			if (Usernames.OrderByDescending(n => n.Date).First().Data == value)
				return;

            Usernames.Add(new InstarUserDataHistoricalEntry<string>(time, value));
        }
    }

    public static InstarUserData CreateFrom(IGuildUser user)
    {
        return new InstarUserData
        {
			GuildID = user.GuildId,
            UserID = user.Id,
            Birthday = null,
            Joined = user.JoinedAt?.UtcDateTime,
            Position = InstarUserPosition.NewMember,
            Avatars =
            [
                new InstarUserDataHistoricalEntry<string>(DateTime.UtcNow, user.GetAvatarUrl(ImageFormat.Auto, 1024) ?? "")
            ],
            Nicknames =
            [
                new InstarUserDataHistoricalEntry<string>(DateTime.UtcNow, user.Nickname)
            ],
            Usernames = [ new InstarUserDataHistoricalEntry<string>(DateTime.UtcNow, user.Username) ]
        };
    }
}

public record AutoMemberHoldRecord
{
	[DynamoDBProperty("date")]
	public DateTime Date { get; set; }

	[DynamoDBProperty("mod", Converter = typeof(InstarSnowflakePropertyConverter))]
	public Snowflake ModeratorID { get; set; }

	[DynamoDBProperty("reason")]
	public string Reason { get; set; }
}

public interface ITimedEvent
{
    DateTime Date { get; }
}

[UsedImplicitly]
public record InstarUserDataHistoricalEntry<T> : ITimedEvent
{
    [DynamoDBProperty("date")]
    public DateTime Date { get; set; }
        
    [DynamoDBProperty("data")]
    public T? Data { get; set; }

    public InstarUserDataHistoricalEntry()
    {
        Date = DateTime.UtcNow;
        Data = default;
    }

    public InstarUserDataHistoricalEntry(DateTime date, T data)
    {
        Date = date;
        Data = data;
    }
}

[UsedImplicitly]
public record InstarUserDataNote
{
    [DynamoDBProperty("content")]
    public string Content { get; set; }
    
    [DynamoDBProperty("date")]
    public DateTime Date { get; set; }
    
    [DynamoDBProperty("mod", Converter = typeof(InstarSnowflakePropertyConverter))]
    public Snowflake ModeratorID { get; set; }
}

[UsedImplicitly]
public record InstarUserDataReports
{
    [DynamoDBProperty("message_content")]
    public string MessageContent { get; set; }
    
    [DynamoDBProperty("reason")]
    public string Reason { get; set; }
    
    [DynamoDBProperty("date")]
    public DateTime Date { get; set; }
    
    [DynamoDBProperty("channel", Converter = typeof(InstarSnowflakePropertyConverter))]
    public Snowflake Channel { get; set; }
    
    [DynamoDBProperty("message", Converter = typeof(InstarSnowflakePropertyConverter))]
    public Snowflake Message { get; set; }
    
    [DynamoDBProperty("by_user", Converter = typeof(InstarSnowflakePropertyConverter))]
    public Snowflake Reporter { get; set; }
}

public record InstarModLogEntry
{
    [DynamoDBProperty("context")]
    public string Context { get; set; }
    
    [DynamoDBProperty("date")]
    public DateTime Date { get; set; }

    [DynamoDBProperty("mod", Converter = typeof(InstarSnowflakePropertyConverter))]
    public Snowflake Moderator { get; set; }
    
    [DynamoDBProperty("reason")]
    public string Reason { get; set; }
    
    [DynamoDBProperty("expiry")]
    public DateTime? Expiry { get; set; }
    
    [DynamoDBProperty("type", Converter = typeof(InstarEnumPropertyConverter<InstarModActionType>))]
    public InstarModActionType Type { get; set; }
}

public enum InstarUserPosition
{
    [EnumMember(Value = "OWNER")]
    Owner,
    [EnumMember(Value = "ADMIN")]
    Admin,
    [EnumMember(Value = "MODERATOR")]
    Moderator,
    [EnumMember(Value = "SENIOR_HELPER")]
    SeniorHelper,
    [EnumMember(Value = "HELPER")]
    Helper,
    [EnumMember(Value = "COMMUNITY_MANAGER")]
    CommunityManager,
    [EnumMember(Value = "MEMBER")]
    Member,
    [EnumMember(Value = "NEW_MEMBER")]
    NewMember,
    [EnumMember(Value = "UNKNOWN")]
    Unknown
}

public enum InstarModActionType
{
    [EnumMember(Value = "BAN")]
    Ban,
    [EnumMember(Value = "KICK")]
    Kick,
    [EnumMember(Value = "MUTE")]
    Mute,
    [EnumMember(Value = "WARN")]
    Warn,
    [EnumMember(Value = "VOICE_MUTE")]
    VoiceMute,
    [EnumMember(Value = "SOFT_BAN")]
    Softban,
    [EnumMember(Value = "VOICE_BAN")]
    Voiceban,
    [EnumMember(Value = "TIMEOUT")]
    Timeout
    
}

public class InstarEnumPropertyConverter<T> : IPropertyConverter where T : Enum
{
    public DynamoDBEntry ToEntry(object value)
    {
        var pos = (InstarUserPosition) value;

        var name = pos.GetAttributeOfType<EnumMemberAttribute>();
        return name?.Value ?? "UNKNOWN";
    }

    public object FromEntry(DynamoDBEntry entry)
    {
        var sEntry = entry.AsString();
        if (sEntry is null || string.IsNullOrWhiteSpace(entry.AsString()))
            return InstarUserPosition.Unknown;
            
        var name = Utilities.ToEnum<T>(sEntry);
        
        return name;
    }
}

public class InstarSnowflakePropertyConverter : IPropertyConverter
{
    public DynamoDBEntry ToEntry(object value)
    {
        return value switch
        {
            Snowflake snowflake => snowflake.ID.ToString(),
            _ => value.ToString()
        };
    }

    public object FromEntry(DynamoDBEntry entry)
    {
        var sEntry = entry.AsString();
        if (sEntry is null || string.IsNullOrWhiteSpace(entry.AsString()) || !ulong.TryParse(sEntry, out var id))
            return new Snowflake(0);
        
        return new Snowflake(id);
    }
}

public class InstarBirthdatePropertyConverter : IPropertyConverter
{
	public DynamoDBEntry ToEntry(object value)
	{
		return value switch
		{
			DateTimeOffset dto => dto.ToString("o"),
			Birthday birthday => birthday.Birthdate.ToString("o"),
			_ => throw new InvalidOperationException("Invalid type for Birthdate conversion.")
		};
	}

	public object FromEntry(DynamoDBEntry entry)
	{
		var sEntry = entry.AsString();
		if (sEntry is null || string.IsNullOrWhiteSpace(entry.AsString()) || !DateTimeOffset.TryParse(sEntry, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dto))
			return new DateTimeOffset();

		return new Birthday(dto, TimeProvider.System);
	}
}