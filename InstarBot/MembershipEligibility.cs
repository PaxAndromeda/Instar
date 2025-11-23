namespace PaxAndromeda.Instar;

[Flags]
public enum MembershipEligibility
{
	Invalid = 0x0,
    Eligible = 0x1,
    NotEligible = 0x2,
    AlreadyMember = 0x4,
    TooYoung = 0x8,
    MissingRoles = 0x10,
    MissingIntroduction = 0x20,
    PunishmentReceived = 0x40,
    NotEnoughMessages = 0x80,
	AutoMemberHold = 0x100
}