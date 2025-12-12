using System.Diagnostics.CodeAnalysis;
using Discord;
using PaxAndromeda.Instar.DynamoModels;

namespace PaxAndromeda.Instar.Services;

[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
public interface IInstarDDBService
{
    /// <summary>
    /// Retrieves user data from DynamoDB for a provided <paramref name="snowflake"/>.
    /// </summary>
    /// <param name="snowflake">The user ID</param>
    /// <returns>User data associated with the provided <paramref name="snowflake"/>, if any exists</returns>
    /// <exception cref="ArgumentException">If the `position` entry does not represent a valid <see cref="InstarUserPosition"/></exception>
    Task<InstarDatabaseEntry<InstarUserData>?> GetUserAsync(Snowflake snowflake);
    
    
    /// <summary>
    /// Retrieves or creates user data from a provided <paramref name="user"/>.
    /// </summary>
    /// <param name="user">An instance of <see cref="IGuildUser"/>. If a new user must be created,
    /// information will be pulled from the <paramref name="user"/> parameter.</param>
    /// <returns>An instance of <see cref="InstarDatabaseEntry{T}"/>.</returns>
    /// <remarks>
    ///     When a new user is created with this method, it is *not* created in DynamoDB until
    ///     <see cref="InstarDatabaseEntry{T}.UpdateAsync"/> is called.
    /// </remarks>
    Task<InstarDatabaseEntry<InstarUserData>> GetOrCreateUserAsync(IGuildUser user);

    /// <summary>
    /// Retrieves a list of user data from a list of <paramref name="snowflakes"/>.
    /// </summary>
    /// <param name="snowflakes">A list of user ID snowflakes to query.</param>
    /// <returns>A list of <see cref="InstarDatabaseEntry{T}"/> containing <see cref="InstarUserData"/> for the provided <paramref name="snowflakes"/>.</returns>
    Task<List<InstarDatabaseEntry<InstarUserData>>> GetBatchUsersAsync(IEnumerable<Snowflake> snowflakes);
    
    /// <summary>
    /// Creates a new user in DynamoDB.
    /// </summary>
    /// <param name="data">An instance of <see cref="InstarUserData"/> to save into DynamoDB.</param>
    /// <returns>Nothing.</returns>
    Task CreateUserAsync(InstarUserData data);

	/// <summary>
	/// Retrieves a list of users whose birthdays match the specified date, allowing for a margin of error defined by the
	/// fuzziness parameter.
	/// </summary>
	/// <remarks>The search includes users whose birthdays are within the specified fuzziness window before or after
	/// the given date. This method performs the comparison in UTC to ensure consistency across time zones.</remarks>
	/// <param name="birthdate">The birthdate to search for. Represents the target date to match  against user birthdays.</param>
	/// <param name="fuzziness">The allowable time range, as a <see cref="TimeSpan"/>, within which user birthdays are considered a match. Must be
	/// non-negative.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
	/// cref="InstarDatabaseEntry{InstarUserData}"/> objects for users whose birthdays fall within the specified range.
	/// Returns an empty list if no users are found.</returns>
	Task<List<InstarDatabaseEntry<InstarUserData>>> GetUsersByBirthday(DateTimeOffset birthdate, TimeSpan fuzziness);
}