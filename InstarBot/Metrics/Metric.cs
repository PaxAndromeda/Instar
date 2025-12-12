using System.Diagnostics.CodeAnalysis;

namespace PaxAndromeda.Instar.Metrics;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum Metric
{
    [MetricDimension("Service", "Paging System")]
    [MetricName("Pages Sent")]
    Paging_SentPages,
    
    [MetricDimension("Service", "Birthday System")]
    [MetricName("Birthdays Set")]
    BS_BirthdaysSet,
    
    [MetricDimension("Service", "User Reporting System")]
    [MetricName("Reported Users")]
    ReportUser_ReportsSent,
    
    [MetricDimension("Service", "Auto Member System")]
    [MetricName("Eligibility Checks")]
    AMS_EligibilityCheck,
    
    [MetricDimension("Service", "Auto Member System")]
    [MetricName("Runs")]
    AMS_Runs,
    
    [MetricDimension("Service", "Auto Member System")]
    [MetricName("Cached Messages")]
    AMS_CachedMessages,
    
    [MetricDimension("Service", "Auto Member System")]
    [MetricName("New Members")]
    AMS_NewMembers,
    
    [MetricDimension("Service", "Auto Member System")]
    [MetricName("Users Granted Membership")]
    AMS_UsersGrantedMembership,

	[MetricDimension("Service", "Auto Member System")]
	[MetricName("DynamoDB Failures")]
	AMS_DynamoFailures,

	[MetricDimension("Service", "Auto Member System")]
	[MetricName("AMH Application Failures")]
	AMS_AMHFailures,

	[MetricDimension("Service", "Birthday System")]
	[MetricName("Birthday System Failures")]
	BirthdaySystem_Failures,

	[MetricDimension("Service", "Birthday System")]
	[MetricName("Birthday Grants")]
	BirthdaySystem_Grants,

	[MetricDimension("Service", "Discord")]
    [MetricName("Messages Sent")]
    Discord_MessagesSent,
    
    [MetricDimension("Service", "Discord")]
    [MetricName("Messages Deleted")]
    Discord_MessagesDeleted,
    
    [MetricDimension("Service", "Discord")]
    [MetricName("Users Joined")]
    Discord_UsersJoined,
    
    [MetricDimension("Service", "Discord")]
    [MetricName("Users Left")]
    Discord_UsersLeft,

	[MetricDimension("Service", "Gaius")]
	[MetricName("Gaius API Calls")]
	Gaius_APICalls,

	[MetricDimension("Service", "Gaius")]
	[MetricName("Gaius API Latency")]
	Gaius_APILatency
}