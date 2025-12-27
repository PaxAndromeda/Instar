using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Amazon.DynamoDBv2.DataModel;
using JetBrains.Annotations;

// Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8618

namespace PaxAndromeda.Instar.DynamoModels;

[DynamoDBTable("InstarNotifications")]
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public class Notification
{
	[DynamoDBHashKey("guild_id", Converter = typeof(InstarSnowflakePropertyConverter))]
	[DynamoDBGlobalSecondaryIndexHashKey("gsi_type_referenceuser", AttributeName = "guild_id")]
	public Snowflake GuildID { get; set; }

	[DynamoDBRangeKey("date")]
	public DateTime Date { get; set; }

	[DynamoDBProperty("type", Converter = typeof(InstarEnumPropertyConverter<NotificationType>))]
	[DynamoDBGlobalSecondaryIndexHashKey("gsi_type_referenceuser", AttributeName = "type")]
	public NotificationType Type { get; set; }

	[DynamoDBProperty("actor", Converter = typeof(InstarSnowflakePropertyConverter))]
	public Snowflake Actor { get; set; }

	[DynamoDBProperty("subject")]
	public string Subject { get; set; }
	
	[DynamoDBProperty("channel", Converter = typeof(InstarSnowflakePropertyConverter))]
	public Snowflake Channel { get; set; }

	[DynamoDBProperty("target")]
	public List<NotificationTarget> Targets { get; set; }

	[DynamoDBProperty("priority", Converter = typeof(InstarEnumPropertyConverter<NotificationPriority>))]
	public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

	[DynamoDBProperty("data")]
	public NotificationData Data { get; set; }

	[DynamoDBProperty("send_attempts")]
	public int SendAttempts { get; set; }

	[DynamoDBProperty("reference_user", Converter = typeof(InstarSnowflakePropertyConverter))]
	[DynamoDBGlobalSecondaryIndexRangeKey("gsi_type_referenceuser", AttributeName = "reference_user")]
	public Snowflake? ReferenceUser { get; set; }
}

public enum NotificationType
{
	[EnumMember(Value = "NORMAL")]
	Normal,

	[EnumMember(Value = "AMH")]
	AutoMemberHold
}

public record NotificationTarget
{
	[DynamoDBProperty("type", Converter = typeof(InstarEnumPropertyConverter<NotificationTargetType>))]
	public NotificationTargetType Type { get; init; }

	[DynamoDBProperty("id", Converter = typeof(InstarSnowflakePropertyConverter))]
	public Snowflake Id { get; init; }
}

public record NotificationData
{
	[DynamoDBProperty("message")]
	public string Message { get; init; }

	[DynamoDBProperty("url")]
	public string? Url { get; [UsedImplicitly] init; }

	[DynamoDBProperty("image_url")]
	public string? ImageUrl { get; [UsedImplicitly] init; }

	[DynamoDBProperty("thumbnail_url")]
	public string? ThumbnailUrl { get; [UsedImplicitly] init; }

	[DynamoDBProperty("fields")]
	public List<NotificationEmbedField>? Fields { get; init; }
}

public record NotificationEmbedField
{
	[DynamoDBProperty("name")]
	public string Name { get; init; }

	[DynamoDBProperty("value")]
	public string Value { get; init; }

	[DynamoDBProperty("inline")]
	public bool? Inline { get; init; }
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