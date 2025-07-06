using Discord.Interactions;
using JetBrains.Annotations;

namespace PaxAndromeda.Instar;

public enum PageTarget
{
    Test,
    
    [TeamRef("ffcf94e3-3080-455a-82e2-7cd9ec7eaafd"), UsedImplicitly]
    Owner,
    [TeamRef("4e484ea5-3cd1-46d4-8fe8-666e34f251ad"), UsedImplicitly]
    Admin,
    [TeamRef("9609125a-7e63-4110-8d50-381230ea11b2"), UsedImplicitly]
    Moderator,
    [TeamRef("521dce27-9ed9-48fc-9615-dc1d77b72fdd"), UsedImplicitly]
    Helper,
    [TeamRef("fe434e9a-2a69-41b6-a297-24e26ba4aebe"), UsedImplicitly]
    [ChoiceDisplay("Community Manager")]
    CommunityManager,

    [TeamRef("ffcf94e3-3080-455a-82e2-7cd9ec7eaafd")]
    [TeamRef("4e484ea5-3cd1-46d4-8fe8-666e34f251ad")]
    [TeamRef("9609125a-7e63-4110-8d50-381230ea11b2")]
    [TeamRef("521dce27-9ed9-48fc-9615-dc1d77b72fdd")]
    [UsedImplicitly]
    All
}