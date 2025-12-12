using Discord;

namespace PaxAndromeda.Instar.Services;

public interface IBirthdaySystem
{
	Task Initialize();
	Task RunAsync();

	/// <summary>
	/// Grants the birthday role to a user outside the normal birthday check process. For example, a
	/// user sets their birthday to today via command.
	/// </summary>
	/// <param name="user">The user to grant the birthday role to.</param>
	/// <param name="birthday">The user's birthday.</param>
	/// <returns>Nothing.</returns>
	Task GrantUnexpectedBirthday(IGuildUser user, Birthday birthday);
}