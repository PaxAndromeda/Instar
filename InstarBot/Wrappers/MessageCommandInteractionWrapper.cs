using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.Interactions;

namespace PaxAndromeda.Instar.Wrappers;

/// <summary>
/// Mock wrapper for <see cref="IMessageCommandInteraction"/>
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Wrapper class")]
[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public class MessageCommandInteractionWrapper(IMessageCommandInteraction interaction) : IInstarMessageCommandInteraction
{
    public virtual ulong Id => interaction.Id;
    public virtual IUser User => interaction.User;
    public virtual IMessageCommandInteractionData Data => interaction.Data;

    public virtual Task RespondWithModalAsync<T>(string customId, RequestOptions options = null!,
        Action<ModalBuilder> modifyModal = null!) where T : class, IModal
    {
        return interaction.RespondWithModalAsync<T>(customId, options, modifyModal);
    }
}