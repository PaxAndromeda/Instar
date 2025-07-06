using System.Reflection;

namespace PaxAndromeda.Instar;

public static class Utilities
{
    public static List<T>? GetAttributesOfType<T>(this Enum enumVal) where T : Attribute
    {
        var type = enumVal.GetType();
        var membersInfo = type.GetMember(enumVal.ToString());
        if (membersInfo.Length == 0)
            return null;

        var attributes = membersInfo[0].GetCustomAttributes(typeof(T), false);
        return attributes.Length > 0 ? attributes.OfType<T>().ToList() : null;
    }
    
    public static T? GetAttributeOfType<T>(this Enum enumVal) where T : Attribute
    {
        var type = enumVal.GetType();
        var membersInfo = type.GetMember(enumVal.ToString());
        return membersInfo.Length == 0 ? null : membersInfo[0].GetCustomAttribute<T>(false);
    }

    public static string Remove(this string text, Range range)
    {
        return text.Remove(range.Start.Value, range.End.Value - range.Start.Value);
    }

    public static Range GetLineBoundaries(string text, int index)
    {
        var lineStart = 0;
        var lineEnd = 0;
        // Find the start of the line and the end of the line
        for (var i = index; i >= 0; i--)
            if (text[i] == '\r' || text[i] == '\n')
            {
                lineStart = i+1;
                break;
            }
        for (var i = index; i < text.Length; i++)
            if (text[i] == '\r' || text[i] == '\n')
            {
                lineEnd = i;
                break;
            }

        if (lineStart > lineEnd)
            throw new InvalidOperationException(
                "Could not remove total cases from API response: lineStart > lineEnd");

        return new Range(new Index(lineStart), new Index(lineEnd));
    }
}