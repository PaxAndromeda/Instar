namespace PaxAndromeda.Instar;

/// <summary>
/// A set of flags to describe a user's eligibility for automatically granted membership.
/// </summary>
[Flags]
public enum MembershipEligibility
{
	/// <summary>
	/// An invalid state.
	/// </summary>
	Invalid = 0x0,

	/// <summary>
	/// The user is eligible for membership.
	/// </summary>
    Eligible = 0x1,

	/// <summary>
	/// The user is already a member.
	/// </summary>
    AlreadyMember = 0x2,

	/// <summary>
	/// The user has not been on the server for long enough.
	/// </summary>
    InadequateTenure = 0x4,

	/// <summary>
	/// The user is missing required roles.
	/// </summary>
    MissingRoles = 0x8,

	/// <summary>
	/// The user has not posted an introduction.
	/// </summary>
    MissingIntroduction = 0x10,

	/// <summary>
	/// The user has received some form of punishment precluding a membership grant.
	/// </summary>
    PunishmentReceived = 0x20,

	/// <summary>
	/// The user has not sent enough messages on the server.
	/// </summary>
    NotEnoughMessages = 0x40,

	/// <summary>
	/// The user's membership has been manually withheld.
	/// </summary>
	AutoMemberHold = 0x80
}