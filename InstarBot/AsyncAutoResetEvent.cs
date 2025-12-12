namespace PaxAndromeda.Instar;

/// <summary>
/// Provides an asynchronous auto-reset event that allows tasks to wait for a signal and ensures
/// that only one waiting task is released per signal.
/// </summary>
/// <remarks>
///		AsyncAutoResetEvent enables coordination between asynchronous operations by allowing tasks to wait
///		until the event is signaled. When Set is called, only one waiting task is released; subsequent calls
///		to WaitAsync will wait until the event is signaled again. This class is thread-safe and can be used
///		in scenarios where asynchronous signaling is required, such as implementing producer-consumer
///		patterns or throttling access to resources.
/// </remarks>
/// <param name="signaled">
///		True to initialize the event in the signaled state so that the first call to WaitAsync completes
///		immediately; otherwise, false to initialize in the non-signaled state.
/// </param>
public sealed class AsyncAutoResetEvent(bool signaled)
{
	private readonly Queue<TaskCompletionSource> _queue = new();

	private bool _signaled = signaled;

	/// <summary>
	/// Asynchronously waits until the signal is set or the operation is canceled.
	/// </summary>
	/// <remarks>If the signal is already set when this method is called, the returned task completes immediately.
	/// Multiple callers may wait concurrently; only one will be released per signal. This method is thread-safe.</remarks>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation before the signal is set. If cancellation is
	/// requested, the returned task will be canceled.</param>
	/// <returns>A task that completes when the signal is set or is canceled if the provided cancellation token is triggered.</returns>
	public Task WaitAsync(CancellationToken cancellationToken = default)
	{
		lock (_queue)
		{
			if (_signaled)
			{
				_signaled = false;
				return Task.CompletedTask;
			}
			else
			{
				var tcs = new TaskCompletionSource();
				if (cancellationToken.CanBeCanceled)
				{
					// If the token is cancelled, cancel the waiter.
					var registration = cancellationToken.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);

					// If the waiter completes or faults, unregister our interest in cancellation.
					tcs.Task.ContinueWith(
						_ => registration.Unregister(),
						cancellationToken,
						TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.NotOnFaulted,
						TaskScheduler.Default);
				}
				_queue.Enqueue(tcs);
				return tcs.Task;
			}
		}
	}

	/// <summary>
	/// Signals the event, allowing one waiting operation to proceed or marking the event as signaled if no operations are
	/// waiting.
	/// </summary>
	/// <remarks>If there are pending operations waiting for the event, one will be released. If no operations are
	/// waiting, the event remains signaled until a future wait occurs. This method is thread-safe and can be called from
	/// multiple threads.</remarks>
	public void Set()
	{
		TaskCompletionSource? toRelease = null;

		lock (_queue)
			if (_queue.Count > 0)
				toRelease = _queue.Dequeue();
			else if (!_signaled)	
				_signaled = true;

		// It's possible that the TCS has already been cancelled.
		toRelease?.TrySetResult();
	}
}