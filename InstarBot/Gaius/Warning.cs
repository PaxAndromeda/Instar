using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PaxAndromeda.Instar.Gaius;

[UsedImplicitly]
public record Warning
{
    [UsedImplicitly] public Snowflake GuildID { get; set; } = null!;
    [UsedImplicitly] public int WarnID { get; set; }
    [UsedImplicitly] public Snowflake UserID { get; set; } = null!;
    [UsedImplicitly] public string Reason { get; set; } = null!;

    [JsonConverter(typeof(UnixDateTimeConverter)), UsedImplicitly]
    public DateTime WarnDate { get; set; }

    [UsedImplicitly] public Snowflake? PardonerID { get; set; }

    [JsonConverter(typeof(UnixDateTimeConverter)), UsedImplicitly]
    public DateTime? PardonDate { get; set; }

    [UsedImplicitly] public Snowflake ModID { get; set; } = null!;
}