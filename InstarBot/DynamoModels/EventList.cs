using System.Collections;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace PaxAndromeda.Instar.DynamoModels;

/// <summary>
/// Represents a chronological sequence of events of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the event, which must implement <see cref="ITimedEvent"/>.</typeparam>
public class EventList<T> : IEnumerable<T>
    where T : ITimedEvent
{
    private readonly SortedSet<T> _backbone;

    /// <summary>
    /// Creates a new <see cref="EventList{T}"/>.
    /// </summary>
    public EventList()
    {
        _backbone = new SortedSet<T>(Comparer<T>.Create((x, y) => x.Date.CompareTo(y.Date)));
    }

    /// <summary>
    /// Creates a new <see cref="EventList{T}"/> from a set of events.
    /// </summary>
    /// <typeparam name="T">The type of the events, which must implement <see cref="ITimedEvent"/>.</typeparam>
    public EventList(IEnumerable<T> events) : this()
    {
        foreach (var item in events)
            Add(item);
    }

    public T? Latest() => _backbone.Max;

    /// <summary>
    /// Inserts an item into the sequence while maintaining chronological order.
    /// </summary>
    /// <param name="item">The item to be inserted into the sequence.</param>
    public void Add(T item)
        => _backbone.Add(item);

    public IEnumerator<T> GetEnumerator() => _backbone.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

[UsedImplicitly]
public class EventListPropertyConverter<T> : IPropertyConverter where T: ITimedEvent
{
    public DynamoDBEntry ToEntry(object value)
    {
        if (value is not IEnumerable<T> enumerable)
            throw new InvalidOperationException("Value is not enumerable");
        
        return DynamoDBList.Create(enumerable);
    }

    public object FromEntry(DynamoDBEntry entry)
    {
        List<Document>? entries = entry.AsListOfDocument();
        if (entries is null)
            return new EventList<T>();

        // Convert `entries` to List<T> here... somehow

        var list = entries.Select(x => JsonConvert.DeserializeObject<T>(x.ToJson())).Where(x => x != null);
        
        
        return new EventList<T>(list!);
    }
}