using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace PaxAndromeda.Instar.DynamoModels;

[DynamoDBTable("InstarNotifications")]
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public class Notification
{
	[DynamoDBHashKey("guild_id", Converter = typeof(InstarSnowflakePropertyConverter))]
	public Snowflake? GuildID { get; set; }

	[DynamoDBRangeKey("date")]
	public DateTime Date { get; set; }

	[DynamoDBProperty("actor", Converter = typeof(InstarSnowflakePropertyConverter))]
	public Snowflake? Actor { get; set; }

	[DynamoDBProperty("subject")]
	public string Subject { get; set; }
	
	[DynamoDBProperty("channel", Converter = typeof(InstarSnowflakePropertyConverter))]
	public Snowflake? Channel { get; set; }

	[DynamoDBProperty("target")]
	public List<NotificationTarget> Targets { get; set; }

	[DynamoDBProperty("priority", Converter = typeof(InstarEnumPropertyConverter<NotificationPriority>))]
	public NotificationPriority? Priority { get; set; } = NotificationPriority.Normal;

	[DynamoDBProperty("data")]
	public NotificationData Data { get; set; }
}

public record NotificationTarget
{
	[DynamoDBProperty("type", Converter = typeof(InstarEnumPropertyConverter<NotificationTargetType>))]
	public NotificationTargetType Type { get; set; }

	[DynamoDBProperty("id", Converter = typeof(InstarSnowflakePropertyConverter))]
	public Snowflake? Id { get; set; }
}

public record NotificationData
{
	[DynamoDBProperty("message")]
	public string? Message { get; set; }
}

public enum NotificationTargetType
{
	[EnumMember(Value = "ROLE")]
	Role,
	[EnumMember(Value = "USER")]
	User
}

public enum NotificationPriority
{
	[EnumMember(Value = "LOW")]
	Low,

	[EnumMember(Value = "NORMAL")]
	Normal,

	[EnumMember(Value = "HIGH")]
	High
}