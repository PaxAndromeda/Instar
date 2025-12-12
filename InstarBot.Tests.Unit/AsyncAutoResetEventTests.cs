using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using PaxAndromeda.Instar;
using Xunit;

namespace InstarBot.Tests;

public class AsyncAutoResetEventTests
{
	[Fact]
	public void WaitAsync_CompletesImmediately_WhenInitiallySignaled()
	{
		// Arrange
		var ev = new AsyncAutoResetEvent(true);

		// Act
		var task = ev.WaitAsync();

		// Assert
		task.IsCompleted.Should().BeTrue();
	}

	[Fact]
	public void Set_ReleasesSingleWaiter()
	{
		var ev = new AsyncAutoResetEvent(false);

		var waiter1 = ev.WaitAsync();
		var waiter2 = ev.WaitAsync();

		// First Set should release only one waiter.
		ev.Set();

		// Ensure waiter1 has completed, but waiter2 is not
		waiter1.IsCompleted.Should().BeTrue();
		waiter2.IsCompleted.Should().BeFalse();

		// Second Set should release the remaining waiter.
		ev.Set();
		waiter2.IsCompleted.Should().BeTrue();
	}

	[Fact]
	public void Set_MarksEventSignaled_WhenNoWaiters()
	{
		var ev = new AsyncAutoResetEvent(false);

		// No waiters now — Set should mark the event signaled so the next WaitAsync completes immediately.
		ev.Set();

		var immediate = ev.WaitAsync();

		// WaitAsync should complete immediately after Set() when there were no waiters
		immediate.IsCompleted.Should().BeTrue();

		// That consumption should reset the event; a subsequent waiter should block.
		var next = ev.WaitAsync();

		next.IsCompleted.Should().BeFalse();
	}

	[Fact]
	public async Task WaitAsync_Cancels_WhenCancellationRequested()
	{
		var ev = new AsyncAutoResetEvent(false);
		using var cts = new CancellationTokenSource();

		var task = ev.WaitAsync(cts.Token);
		await cts.CancelAsync();

		await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
	}
}