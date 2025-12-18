using Amazon.DynamoDBv2.DataModel;

namespace PaxAndromeda.Instar.DynamoModels;

/// <summary>
/// Represents a database entry in the Instar application.
/// Provides functionality to encapsulate data and interact with a DynamoDB database context.
/// </summary>
/// <typeparam name="T">The type of the data stored in the database entry.</typeparam>
public sealed class InstarDatabaseEntry<T>(IDynamoDBContext context, T data)
{
    /// <summary>
    /// Represents a property that encapsulates the core data of a database entry.
    /// This property holds the data model for the entry and is used within the context
    /// of DynamoDB operations to save or update information in the associated table.
    /// </summary>
    public T Data { get; } = data;

    /// <summary>
    /// Updates the corresponding database entry for the data encapsulated in the instance.
    /// Persists changes to the underlying storage system asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task"/> that can be used to poll or wait for results, or both</returns>
    public Task CommitAsync()
        => context.SaveAsync(Data);

	/// <summary>
	/// Asynchronously deletes the associated data from the database.
	/// </summary>
	/// <returns>A <see cref="Task"/> that can be used to poll or wait for results, or both</returns>
	public Task DeleteAsync()
		=> context.DeleteAsync(Data);
}