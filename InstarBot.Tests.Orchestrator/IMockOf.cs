using Moq;

namespace InstarBot.Test.Framework;

/// <summary>
/// Represents an object that provides access to the underlying mock for a specified type.
/// </summary>
/// <remarks>This interface is typically used to expose the mock instance associated with a particular type,
/// enabling advanced configuration or verification in unit tests. It is commonly implemented by test doubles or helper
/// classes that encapsulate a mock object.</remarks>
/// <typeparam name="T">The type of the object being mocked. Must be a reference type.</typeparam>
public interface IMockOf<T> where T : class
{
	Mock<T> Mock { get; }
}