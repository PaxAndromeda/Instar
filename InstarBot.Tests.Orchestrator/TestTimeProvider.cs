namespace InstarBot.Test.Framework;

public class TestTimeProvider : TimeProvider
{
	public new static TestTimeProvider System => new();

	private DateTimeOffset? _time;

	public TestTimeProvider()
	{
		_time = null;
	}

	public TestTimeProvider(DateTimeOffset time)
	{
		SetTime(time);
	}

	public void SetTime(DateTimeOffset time)
	{
		_time = time;
	}

	public override DateTimeOffset GetUtcNow()
	{
		return _time?.ToUniversalTime() ?? DateTimeOffset.UtcNow;
	}
}