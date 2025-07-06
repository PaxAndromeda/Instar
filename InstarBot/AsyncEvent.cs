using System.Collections.Immutable;
using Ardalis.GuardClauses;

namespace PaxAndromeda.Instar;

internal sealed class AsyncEvent<T>
{
    private readonly Lock _subLock = new();
    private ImmutableArray<Func<T, Task>> _subscriptions = [];

    public async Task Invoke(T parameter)
    {
        Func<T, Task>[] subCopy;
        // 
        lock (_subLock)
        {
            subCopy = new Func<T, Task>[_subscriptions.Length];
            _subscriptions.CopyTo(subCopy);
        }
        
        foreach (var subscription in subCopy)
            await subscription(parameter);
    }

    public void Add(Func<T, Task> subscriber)
    {
        Guard.Against.Null(subscriber, nameof(subscriber));
        lock (_subLock)
            _subscriptions = _subscriptions.Add(subscriber);
    }

    public void Remove(Func<T, Task> subscriber)
    {
        Guard.Against.Null(subscriber, nameof(subscriber));
        lock (_subLock)
            _subscriptions = _subscriptions.Remove(subscriber);
    }
}