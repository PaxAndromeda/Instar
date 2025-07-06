using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Ardalis.GuardClauses;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PaxAndromeda.Instar.Converters;

namespace PaxAndromeda.Instar;

/// <summary>
/// Represents a snowflake.
/// </summary>
/// <remarks>
///     Snowflakes are encoded in the following way:
///
///     Timestamp                                  Wrkr  Prcs  Increment
///     111111111111111111111111111111111111111111 11111 11111 111111111111
///     64                                         22    17    12          0
///
///     Timestamp is the milliseconds since Discord Epoch, the first second of 2015, or 1420070400000
///     Worker ID is the internal worker that generated the ID
///     Process ID is the internal process that ran the worker
///     Increment is a rolling number on the process.  For every ID that is generated on that process,
///         this number is incremented.
/// </remarks>
[TypeConverter(typeof(SnowflakeConverter))]
[JsonConverter(typeof(JSnowflakeConverter))]
public sealed record Snowflake
{
    private static int _increment;

    /// <summary>
    /// The Discord epoch, defined as the first second of the year 2015.
    /// </summary>
    private const long DiscordEpoch = 1420070400000;

    /// <summary>
    /// The time this snowflake was generated.
    /// </summary>
    public DateTime Time { get; }

    /// <summary>
    /// The internal worker ID that generated this snowflake.
    /// </summary>
    public int InternalWorkerId { get; }

    /// <summary>
    /// The internal process ID that hosted the worker that generated this snowflake.
    /// </summary>
    public int InternalProcessId { get; }

    /// <summary>
    /// The generated ID
    /// </summary>
    /// <remarks>
    ///     For every ID that is generated on that process, this number is incremented
    /// </remarks>
    public int GeneratedId { get; }

    /// <summary>
    /// The raw value of the snowflake.
    /// </summary>
    public ulong ID { get; }

    /// <summary>
    /// Creates a new snowflake from the current time.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "This constructor only provides DateTime.UtcNow.")]
    public Snowflake()
        : this(DateTime.UtcNow)
    {
    }

    /// <summary>
    /// Creates a new snowflake from the provided parameters.
    /// </summary>
    /// <param name="time">The snowflake's generation time</param>
    /// <param name="internalWorkerId">The internal worker ID that generated this snowflake</param>
    /// <param name="internalProcessId">The internal process ID that hosted the worker that generated this snowflake</param>
    /// <param name="generatedId">The incremented ID</param>
    [UsedImplicitly]
    public Snowflake(DateTime time, int internalWorkerId = 0, int internalProcessId = 0, int generatedId = 0)
    {
        // Discord snowflakes always use UTC time
        if (time.Kind != DateTimeKind.Utc)
            time = time.ToUniversalTime();

        // If Discord is actually around in 3015, update the maximum limit on this guard.
        // ...also, hello from the past!
        Guard.Against.OutOfRange(time.Year, nameof(time), 2015, 3015, "Provided datetime is prior to Discord epoch!");
        Guard.Against.OutOfRange(internalWorkerId, nameof(internalWorkerId), 0, 31,
            "Internal worker ID should be between 0 and 31.");
        Guard.Against.OutOfRange(internalProcessId, nameof(internalProcessId), 0, 31,
            "Internal process ID should be between 0 and 31.");
        Guard.Against.OutOfRange(generatedId, nameof(generatedId), 0, 4095,
            "Generated ID should be between 0 and 4095.");

        Time = time;
        InternalWorkerId = internalWorkerId;
        InternalProcessId = internalProcessId;
        GeneratedId = generatedId;

        var ms = (long)(time - DateTime.UnixEpoch).TotalMilliseconds;

        ID = (ulong)generatedId;
        ID |= (ulong)internalProcessId << 12;
        ID |= (ulong)internalWorkerId << 17;
        ID |= (ulong)(ms - DiscordEpoch) << 22;
    }

    /// <summary>
    /// Creates a new <see cref="Snowflake"/> from the provided raw value.
    /// </summary>
    /// <param name="id">The raw snowflake value.</param>
    public Snowflake(ulong id)
    {
        ID = id;

        var msSinceDiscordEpoch = unchecked(((long)id & (long)0xFFFFFFFFFFC00000) >> 22);

        Guard.Against.NegativeOrZero(msSinceDiscordEpoch, nameof(id),
            "Provided snowflake is prior to Discord epoch!");

        var unixTimestamp = msSinceDiscordEpoch + DiscordEpoch;

        Time = new DateTime(1970, 1, 1).AddMilliseconds(unixTimestamp);
        InternalWorkerId = (int)((id & 0x3E0000) >> 17);
        InternalProcessId = (int)((id & 0x1F000) >> 12);
        GeneratedId = (int)(id & 0xFFF);
    }

    public static implicit operator ulong(Snowflake snowflake)
    {
        return snowflake.ID;
    }

    public static implicit operator Snowflake(ulong userId)
    {
        return new Snowflake(userId);
    }

    public static implicit operator DateTime(Snowflake snowflake)
    {
        return snowflake.Time;
    }

    /// <summary>
    /// Generates a new snowflake from the current time.
    /// </summary>
    /// <returns>The generated snowflake.</returns>
    /// <remarks>
    ///     For generated IDs, the increment starts at 0 and increments to 
    /// </remarks>
    public static Snowflake Generate()
    {
        var newIncrement = Interlocked.Increment(ref _increment);

        // Gracefully handle overflows
        if (newIncrement == int.MinValue)
        {
            newIncrement = 0;
            Interlocked.Exchange(ref _increment, 0);
        }

        // Limit to 4096
        newIncrement %= 4096;

        var processId = Environment.ProcessId % 0x2;
        var workerId = Environment.CurrentManagedThreadId % 0x2;

        // Bitmasks are here to provide double redundancy 
        return new Snowflake(DateTime.UtcNow, workerId & 0x1F, processId & 0x1F, newIncrement & 0xFFF);
    }

    /// <summary>
    /// Returns a Discord mention string for a given selected Snowflake.
    /// </summary>
    /// <remarks>
    /// The property selector is used to obtain the <see cref="SnowflakeTypeAttribute"/> associated
    /// with the property.  This attribute declares the type of resource the snowflake is referencing.
    /// </remarks>
    /// <param name="propertySelector">A selector to the Snowflake property to mention</param>
    /// <returns>A standard Discord mention string for the provided snowflake</returns>
    /// <exception cref="ArgumentException">If the selected item is not a field or property</exception>
    /// <exception cref="InvalidOperationException">If the selected property does not have a <see cref="SnowflakeTypeAttribute"/></exception>
    public static string GetMention(Expression<Func<Snowflake>> propertySelector)
    {
        Guard.Against.Null(propertySelector);

        if (propertySelector.Body is not MemberExpression memberExpression)
            throw new ArgumentException("Selector must refer to a member", nameof(propertySelector));

        var snowflakeTypeAttribute = memberExpression.Member.GetCustomAttribute<SnowflakeTypeAttribute>();
        if (snowflakeTypeAttribute is null)
            throw new InvalidOperationException(
                "Cannot get the mention for a member not containing a SnowflakeType attribute");

        var snowflakeType = snowflakeTypeAttribute.Type;
        var snowflake = propertySelector.Compile()();
        return snowflakeType switch
        {
            SnowflakeType.User => $"<@{snowflake.ID}>",
            SnowflakeType.Role => $"<@&{snowflake.ID}>",
            SnowflakeType.Channel => $"<#{snowflake.ID}>",
            _ => throw new InvalidOperationException($"Cannot mention ID type {snowflakeType}")
        };
    }

    public bool Equals(Snowflake? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ID == other.ID;
    }

    public override int GetHashCode()
    {
        return ID.GetHashCode();
    }
}