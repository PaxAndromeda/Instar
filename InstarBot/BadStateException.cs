namespace PaxAndromeda.Instar;

/// <summary>
///		Represents an exception that is thrown when an operation encounters an invalid or unexpected state.
/// </summary>
/// <remarks>
///		Use this exception to indicate that a method or process cannot proceed due to the current state of
///		the object or system. This exception is typically thrown when a precondition for an operation is not met, and
///		recovery may require correcting the state before retrying.
/// </remarks>
public class BadStateException : Exception
{
	public BadStateException()
	{
	}

	public BadStateException(string message) : base(message)
	{
	}

	public BadStateException(string message, Exception inner) : base(message, inner)
	{
	}
}