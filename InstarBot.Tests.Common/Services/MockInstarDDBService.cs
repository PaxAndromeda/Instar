using System.Linq.Expressions;
using Amazon.DynamoDBv2.DataModel;
using Discord;
using Moq;
using Moq.Language.Flow;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Services;

namespace InstarBot.Tests.Services;

/// <summary>
/// A mock implementation of the <c>IInstarDDBService</c> interface for unit testing purposes.
/// This class just uses Moq in the background to provide mockable behavior.
/// </summary>
/// <remarks>
///		Implementation warning: MockInstarDDBService differs from the actual implementation of
///     InstarDDBService.  All returned items from <see cref="GetUserAsync"/> are <i>references</i>,
///     meaning any data set on them will persist for future calls.  This is different from the
///     concrete implementation, in which you would need to call <see cref="InstarDatabaseEntry{T}.UpdateAsync"/> to
///     persist changes.
/// </remarks>
public class MockInstarDDBService : IInstarDDBService
{
	private readonly Mock<IDynamoDBContext> _ddbContextMock = new ();
	private readonly Mock<IInstarDDBService> _internalMock = new ();

    public MockInstarDDBService()
    {
		_internalMock.Setup(n => n.GetUserAsync(It.IsAny<Snowflake>()))
			.ReturnsAsync((InstarDatabaseEntry<InstarUserData>?) null);
	}

    public MockInstarDDBService(IEnumerable<InstarUserData> preload)
		: this()
    {
		foreach (var data in preload) {

			_internalMock
				.Setup(n => n.GetUserAsync(data.UserID!))
				.ReturnsAsync(new InstarDatabaseEntry<InstarUserData>(_ddbContextMock.Object, data));
		}
    }

    public void Register(InstarUserData data)
    {
		_internalMock
				.Setup(n => n.GetUserAsync(data.UserID!))
				.ReturnsAsync(new InstarDatabaseEntry<InstarUserData>(_ddbContextMock.Object, data));
	}

    public Task<InstarDatabaseEntry<InstarUserData>?> GetUserAsync(Snowflake snowflake)
    {
		return _internalMock.Object.GetUserAsync(snowflake);
    }

    public async Task<InstarDatabaseEntry<InstarUserData>> GetOrCreateUserAsync(IGuildUser user)
    {
		// We can't directly set up this method for mocking due to the custom logic here.
		// To work around this, we'll first call the same method on the internal mock. If
		// it returns a value, we return that.
		var mockedResult = await _internalMock.Object.GetOrCreateUserAsync(user);

		// .GetOrCreateUserAsync is expected to never return null in production. However,
		// with mocks, it CAN return null if the method was not set up.
		//
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (mockedResult is not null)
			return mockedResult;

		var result = await _internalMock.Object.GetUserAsync(user.Id);

		if (result is not null)
			return result;

		// Gotta set up the mock
		await CreateUserAsync(InstarUserData.CreateFrom(user));
		result = await _internalMock.Object.GetUserAsync(user.Id);

		if (result is null)
			Assert.Fail("Failed to correctly set up mocks in MockInstarDDBService");

		return result;
	}

    public async Task<List<InstarDatabaseEntry<InstarUserData>>> GetBatchUsersAsync(IEnumerable<Snowflake> snowflakes)
    {
		return await GetLocalUsersAsync(snowflakes).ToListAsync();
    }

	private async IAsyncEnumerable<InstarDatabaseEntry<InstarUserData>> GetLocalUsersAsync(IEnumerable<Snowflake> snowflakes)
	{
		foreach (var snowflake in snowflakes)
		{
			var data = await _internalMock.Object.GetUserAsync(snowflake);

			if (data is not null)
				yield return data;
		}
	}

    public Task CreateUserAsync(InstarUserData data)
    {
		_internalMock
				.Setup(n => n.GetUserAsync(data.UserID!))
				.ReturnsAsync(new InstarDatabaseEntry<InstarUserData>(_ddbContextMock.Object, data));

		return Task.CompletedTask;
	}

	/// <summary>
	/// Configures a setup for the specified expression on the mocked <see cref="IInstarDDBService"/> interface, allowing
	/// control over the behavior of the mock for the given member.
	/// </summary>
	/// <remarks>Use this method to define expectations or return values for specific members of <see
	/// cref="IInstarDDBService"/> when using mocking frameworks. The returned setup object allows chaining of additional
	/// configuration methods, such as specifying return values or verifying calls.</remarks>
	/// <typeparam name="TResult">The type of the value returned by the member specified in the expression.</typeparam>
	/// <param name="expression">An expression that identifies the member of <see cref="IInstarDDBService"/> to set up. Typically, a lambda
	/// expression specifying a method or property to mock.</param>
	/// <returns>An <see cref="ISetup{IInstarDDBService, TResult}"/> instance that can be used to further configure the behavior of
	/// the mock for the specified member.</returns>
    public ISetup<IInstarDDBService, TResult> Setup<TResult>(Expression<Func<IInstarDDBService, TResult>> expression)
	{
		return _internalMock.Setup(expression);
	}
}