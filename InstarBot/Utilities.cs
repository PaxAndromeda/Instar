using System.Reflection;
using System.Runtime.Serialization;

namespace PaxAndromeda.Instar;

public static class EnumExtensions
{
    private static class EnumCache<T> where T : Enum
    {
        public static readonly IReadOnlyDictionary<string, T> Map =
            typeof(T)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(f => (
                    Field: f,
                    Value: f.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? f.Name,
                    EnumValue: (T)f.GetValue(null)!
                ))
                .ToDictionary(
                    x => x.Value,
                    x => x.EnumValue,
                    StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Attempts to parse a string representation of an enum value or its associated
    /// <see cref="EnumMemberAttribute"/> value to its corresponding enum value.
    /// </summary>
    /// <typeparam name="T">The type of the enum to parse.</typeparam>
    /// <param name="value">The string representation of the enum value or its associated
    /// <see cref="EnumMemberAttribute"/> value.</param>
    /// <param name="result">When this method returns, contains the enum value if parsing succeeded;
    /// otherwise, the default value of the enum.</param>
    /// <returns>
    /// <c>true</c> if the string value was successfully parsed to an enum value;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool TryParseEnumMember<T>(string value, out T result) where T : Enum
    {
        // NULLABILITY: result can be null if TryGetValue returns false
        return EnumCache<T>.Map.TryGetValue(value, out result!);
    }

    /// <summary>
    /// Attempts to parse a string representation of an enum value or its associated
    /// <see cref="EnumMemberAttribute"/> value to its corresponding enum value. Throws an exception
    /// if the specified value cannot be parsed.
    /// </summary>
    /// <typeparam name="T">The type of the enum to parse.</typeparam>
    /// <param name="value">The string representation of the enum value or its associated
    /// <see cref="EnumMemberAttribute"/> value.</param>
    /// <returns>Returns the corresponding enum value of type <typeparamref name="T"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the specified value does not match any enum value
    /// or associated <see cref="EnumMemberAttribute"/> value in the enum type <typeparamref name="T"/>.</exception>
    public static T ParseEnumMember<T>(string value) where T : Enum
    {
	    return TryParseEnumMember(value, out T result)
			? result
			: throw new ArgumentException($"Value '{value}' not found in enum {typeof(T).Name}");
    }

    /// <summary>
    /// Retrieves the string representation of an enum value as defined by its associated
    /// <see cref="EnumMemberAttribute"/> value, or the enum value's name if no attribute is present.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <param name="value">The enum value to retrieve the string representation for.</param>
    /// <returns>
    /// The string representation of the enum value as defined by the <see cref="EnumMemberAttribute"/>,
    /// or the enum value's name if no attribute is present.
    /// </returns>
    public static string GetEnumMemberValue<T>(this T value) where T : Enum
    {
        return EnumCache<T>.Map
            .FirstOrDefault(x => EqualityComparer<T>.Default.Equals(x.Value, value))
            .Key ?? value.ToString();
    }
}

public static class Utilities
{
    /// <summary>
    /// Retrieves a list of attributes of a specified type <typeparamref name="T"/> defined on an enum value <paramref name="enumVal"/>.
    /// </summary>
    /// <typeparam name="T">The type of the attribute to retrieve.</typeparam>
    /// <param name="enumVal">The enum value whose attributes are to be retrieved.</param>
    /// <returns>
    /// A list of attributes of the specified type associated with the enum value;
    /// or <c>null</c> if no attributes of the specified type are found.
    /// </returns>
    public static List<T>? GetAttributesOfType<T>(this Enum enumVal) where T : Attribute
    {
        var type = enumVal.GetType();
        var membersInfo = type.GetMember(enumVal.ToString());
        if (membersInfo.Length == 0)
            return null;

        var attributes = membersInfo[0].GetCustomAttributes(typeof(T), false);
        return attributes.Length > 0 ? attributes.OfType<T>().ToList() : null;
    }

    /// <summary>
    /// Retrieves the first custom attribute of the specified type <typeparamref name="T"/> applied to the
    /// member that corresponds to the given enum value <paramref name="enumVal"/>.
    /// </summary>
    /// <typeparam name="T">The type of attribute to retrieve.</typeparam>
    /// <param name="enumVal">The enum value whose member's custom attribute is retrieved.</param>
    /// <returns>
    /// The first custom attribute of type <typeparamref name="T"/> if found;
    /// otherwise, <c>null</c>.
    /// </returns>
    public static T? GetAttributeOfType<T>(this Enum enumVal) where T : Attribute
    {
        var type = enumVal.GetType();
        var membersInfo = type.GetMember(enumVal.ToString());
        return membersInfo.Length == 0 ? null : membersInfo[0].GetCustomAttribute<T>(false);
    }

    /// <summary>
    /// Converts the string representation of an enum value or its associated
    /// <see cref="EnumMemberAttribute"/> value to its corresponding enum value of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the enum to convert to.</typeparam>
    /// <param name="name">The string representation of the enum value or its associated
    /// <see cref="EnumMemberAttribute"/> value.</param>
    /// <returns>The corresponding enum value of type <typeparamref name="T"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the specified string does not match any
    /// enum value or associated <see cref="EnumMemberAttribute"/> value in the enum type <typeparamref name="T"/>.</exception>
    public static T ToEnum<T>(string name) where T : Enum
    {
        return EnumExtensions.ParseEnumMember<T>(name);
    }

    /// <summary>
    /// Converts a string in <c>SCREAMING_SNAKE_CASE</c> format to <c>PascalCase</c> format.
    /// </summary>
    /// <param name="input">The input string in <c>SCREAMING_SNAKE_CASE</c> format.</param>
    /// <returns>
    /// A string converted from <c>SCREAMING_SNAKE_CASE</c> to <c>PascalCase</c> format.
    /// </returns>
    public static string ScreamingToPascalCase(string input)
    {
        // COMMUNITY_MANAGER ⇒ CommunityManager
        return input.Split('_').Select(piece => piece[0] + piece[1..].ToLower()).Aggregate((a, b) => a + b);
    }
}