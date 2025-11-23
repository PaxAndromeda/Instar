using Discord;
using PaxAndromeda.Instar.ConfigModels;

namespace PaxAndromeda.Instar.Services;

public interface IAutoMemberSystem
{
	Task RunAsync();

	/// <summary>
	/// Determines the eligibility of a user for membership based on specific criteria.
	/// </summary>
	/// <param name="cfg">The current configuration from AppConfig.</param>
	/// <param name="user">The user whose eligibility is being evaluated.</param>
	/// <returns>An enumeration value of type <see cref="MembershipEligibility"/> that indicates the user's membership eligibility status.</returns>
	/// <remarks>
	///     The criteria for membership is as follows:
	/// <list type="bullet">
	///     <item>The user must have the required roles (see <see cref="AutoMemberSystem.CheckUserRequiredRoles"/>)</item>
	///     <item>The user must be on the server for a configurable minimum amount of time</item>
	///     <item>The user must have posted an introduction</item>
	///     <item>The user must have posted enough messages in a configurable amount of time</item>
	///     <item>The user must not have been issued a moderator action</item>
	///     <item>The user must not already be a member</item>
	/// </list>
	/// </remarks>
	MembershipEligibility CheckEligibility(InstarDynamicConfiguration cfg, IGuildUser user);
}