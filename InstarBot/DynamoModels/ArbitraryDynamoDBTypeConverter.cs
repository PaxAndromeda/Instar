using System.Collections;
using System.Reflection;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace PaxAndromeda.Instar.DynamoModels;

public class ArbitraryDynamoDBTypeConverter<T>: IPropertyConverter where T: new()
{
    public DynamoDBEntry ToEntry(object value)
    {
        /*
         * Object to DynamoDB Entry
         *
         * For this conversion, we will only look for public properties.
         * We will apply a 1:1 conversion between property names and DynamoDB properties.
         * We will also apply this recursively as needed (with a max definable depth) to
         * prevent loop conditions.
         *
         * A few property attributes we need to be aware of are DynamoDBProperty and its
         * derivatives, and DynamoDBIgnore.  We can ignore any property with
         * a DynamoDBIgnore attribute.
         *
         * Any attribute that inherits from DynamoDBProperty will be handled using special
         * logic: The property name can be substituted by the attribute's `AttributeName`
         * property (if it exists), and a special converter may be used as well (if it exists).
         *
         * We will do no special handling for epoch conversions.  If no converter is defined,
         * we will either handle it natively if it is a primitive type or otherwise known
         * to DynamoDBv2 SDK, otherwise we'll just apply this arbitrary type converter.
         */

        return ToDynamoDbEntry(value);
    }

    public object? FromEntry(DynamoDBEntry entry)
    {
        return FromDynamoDBEntry<T>(entry.AsDocument());
    }

    private static Document ToDynamoDbEntry(object obj, int maxDepth = 3, int currentDepth = 0)
    {
        ArgumentNullException.ThrowIfNull(obj);

        if (currentDepth > maxDepth)
            throw new InvalidOperationException("Max recursion depth reached");

        var doc = new Document();

        // Loop through all public properties of the object
        foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Ignore write-only or non-readable properties
            if (!property.CanRead) continue;

            // Handle properties marked with [DynamoDBIgnore]
            if (Attribute.IsDefined(property, typeof(DynamoDBIgnoreAttribute)))
                continue;

            var propertyName = property.Name;

            // Check if a DynamoDBProperty is defined on the property
            if (Attribute.IsDefined(property, typeof(DynamoDBPropertyAttribute)))
            {
                var dynamoDbProperty = property.GetCustomAttribute<DynamoDBPropertyAttribute>();
                if (!string.IsNullOrEmpty(dynamoDbProperty?.AttributeName))
                {
                    propertyName = dynamoDbProperty.AttributeName;
                }
            }

            var propertyValue = property.GetValue(obj);
            if (propertyValue == null) continue;

            // Check for converters
            var converterAttr = property.GetCustomAttribute<DynamoDBPropertyAttribute>()?.Converter;
            if (converterAttr != null && Activator.CreateInstance(converterAttr) is IPropertyConverter converter)
            {
                doc[propertyName] = converter.ToEntry(propertyValue);
            }
            else
            {
                // Perform recursive or native handling
                doc[propertyName] = ConvertToDynamoDbValue(propertyValue, maxDepth, currentDepth + 1);
            }
        }

        return doc;
    }

    private static DynamoDBEntry ConvertToDynamoDbValue(object? value, int maxDepth, int currentDepth)
    {
        switch (value)
        {
	        case null:
		        return new Primitive();
	        // Handle primitive types natively supported by DynamoDB
	        case string:
	        case bool:
	        case int:
	        case long:
	        case short:
	        case double:
	        case float:
	        case decimal:
		        return new Primitive
		        {
			        Value = value
		        };
	        // Handle DateTime
	        case DateTime dateTimeVal:
		        return new Primitive
		        {
			        Value = dateTimeVal.ToString("o")
		        };
	        // Handle collections (e.g., arrays, lists)
	        case IEnumerable enumerable:
	        {
		        var list = new DynamoDBList();
		        foreach (var element in enumerable)
		        {
			        list.Add(ConvertToDynamoDbValue(element, maxDepth, currentDepth));
		        }
		        return list;
	        }
        }

        // Handle objects recursively
        return value.GetType().IsClass 
			? ToDynamoDbEntry(value, maxDepth, currentDepth) 
			: throw new InvalidOperationException($"Cannot convert type {value.GetType()} to DynamoDBEntry.");
    }
    
    public static TObj FromDynamoDBEntry<TObj>(Document document, int maxDepth = 3, int currentDepth = 0) where TObj : new()
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        if (currentDepth > maxDepth)
            throw new InvalidOperationException("Max recursion depth reached.");

        var obj = new TObj();

        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Ignore write-only or non-readable properties
            if (!property.CanWrite) continue;

            // Skip properties marked with [DynamoDBIgnore]
            if (Attribute.IsDefined(property, typeof(DynamoDBIgnoreAttribute)))
                continue;

            var propertyName = property.Name;

            // Look for [DynamoDBProperty] to handle custom mappings
            if (Attribute.IsDefined(property, typeof(DynamoDBPropertyAttribute)))
            {
                var dynamoDBProperty = property.GetCustomAttribute<DynamoDBPropertyAttribute>();
                if (!string.IsNullOrWhiteSpace(dynamoDBProperty?.AttributeName))
                    propertyName = dynamoDBProperty.AttributeName;
            }

            // Check if the document contains the property
            if (!document.TryGetValue(propertyName, out DynamoDBEntry? entry)) continue;

            var converterAttr = property.GetCustomAttribute<DynamoDBPropertyAttribute>()?.Converter;

            if (converterAttr != null && Activator.CreateInstance(converterAttr) is IPropertyConverter converter)
            {
                // Use the custom converter to deserialize
                property.SetValue(obj, converter.FromEntry(entry));
            }
            else
            {
                // Perform recursive or default conversion
                var convertedValue = FromDynamoDBValue(property.PropertyType, entry, maxDepth, currentDepth + 1);
                property.SetValue(obj, convertedValue);
            }
        }

        return obj;
    }

    private static object? FromDynamoDBValue(Type targetType, DynamoDBEntry entry, int maxDepth, int currentDepth)
    {
        if (entry is Primitive primitive)
        {
            // Handle primitive types
            if (targetType == typeof(string)) return primitive.AsString();
            if (targetType == typeof(bool)) return primitive.AsBoolean();
            if (targetType == typeof(int)) return primitive.AsInt();
            if (targetType == typeof(long)) return primitive.AsLong();
            if (targetType == typeof(short)) return primitive.AsShort();
            if (targetType == typeof(double)) return primitive.AsDouble();
            if (targetType == typeof(float)) return primitive.AsSingle();
            if (targetType == typeof(decimal)) return Convert.ToDecimal(primitive.Value);
            if (targetType == typeof(DateTime)) return DateTime.Parse(primitive.AsString());

            throw new InvalidOperationException($"Unhandled primitive type conversion: {targetType}");
        }

        if (entry is DynamoDBList list)
        {
            if (typeof(IEnumerable).IsAssignableFrom(targetType))
            {
                var elementType = targetType.IsArray
                    ? targetType.GetElementType()
                    : targetType.GetGenericArguments().FirstOrDefault();
                if (elementType == null)
                    throw new InvalidOperationException($"Cannot determine element type for target type: {targetType}");

                var enumerableType = typeof(List<>).MakeGenericType(elementType);
                if (Activator.CreateInstance(enumerableType) is not IList resultList)
					throw new InvalidOperationException($"Failed to create an IList of target type {targetType}");
                foreach (var element in list.Entries)
                {
                    resultList.Add(FromDynamoDBValue(elementType, element, maxDepth, currentDepth));
                }

                return targetType.IsArray ? Activator.CreateInstance(targetType, resultList) : resultList;
            }
        }

        if (entry is Document document)
        {
            if (targetType.IsClass)
            {
                // Recurse for nested objects
                var fromEntryMethod = typeof(ArbitraryDynamoDBTypeConverter<T>).GetMethod(nameof(FromDynamoDBEntry),
                    BindingFlags.Public | BindingFlags.Static)
                    ?.MakeGenericMethod(targetType);
                if (fromEntryMethod == null)
                    throw new InvalidOperationException(
                        $"Unable to deserialize nested type: {targetType}");

                return fromEntryMethod.Invoke(null, [ document, maxDepth, currentDepth ]);
            }
        }

        // Unsupported or unknown type
        throw new InvalidOperationException($"Cannot convert DynamoDB entry to type: {targetType}");
    }

}