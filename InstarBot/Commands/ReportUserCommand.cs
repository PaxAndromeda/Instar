using System.Diagnostics.CodeAnalysis;
using System.Runtime.Caching;
using Ardalis.GuardClauses;
using Discord;
using Discord.Interactions;
using PaxAndromeda.Instar.Embeds;
using PaxAndromeda.Instar.Metrics;
using PaxAndromeda.Instar.Modals;
using PaxAndromeda.Instar.Services;
using Serilog;

namespace PaxAndromeda.Instar.Commands;

// Required to be unsealed for mocking
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
public class ReportUserCommand(IDynamicConfigService dynamicConfig, IMetricService metricService) : BaseCommand, IContextCommand
{
    private const string ModalId = "respond_modal";

    private static readonly MemoryCache Cache = new("User Report Cache");

    internal static void PurgeCache()
    {
        foreach (var n in Cache)
            Cache.Remove(n.Key, CacheEntryRemovedReason.Removed);
    }

    [ExcludeFromCodeCoverage(Justification = "Constant used for mapping")]
    public string Name => "Report Message";

    public async Task HandleCommand(IInstarMessageCommandInteraction arg)
    {
        // Cache the message the user is trying to report
        Cache.Set(arg.User.Id.ToString(), arg.Data.Message,
            new CacheItemPolicy
            {
                SlidingExpiration = TimeSpan.FromMinutes(5)
            });

        await arg.RespondWithModalAsync<ReportMessageModal>(ModalId);
    }

    [ExcludeFromCodeCoverage(Justification = "Purely a creation utility method")]
    public MessageCommandProperties CreateCommand()
    {
        Log.Verbose("Registering ReportUserCommand...");
        var reportMessageCommand = new MessageCommandBuilder()
            .WithName(Name);

        return reportMessageCommand.Build();
    }

    [ModalInteraction(ModalId)]
    public async Task ModalResponse(ReportMessageModal modal)
    {
        var message = (IMessage?) Cache.Get(Context.User!.Id.ToString());
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (message is null)
        {
            await RespondAsync(Strings.Command_ReportUser_ReportExpired, ephemeral: true);
            return;
        }

        await SendReportMessage(modal, message, Context.Guild);

        await RespondAsync(Strings.Command_ReportUser_ReportSent, ephemeral: true);
    }

	private async Task SendReportMessage(ReportMessageModal modal, IMessage message, IInstarGuild guild)
	{
		Guard.Against.Null(Context.User);

		var cfg = await dynamicConfig.GetConfig();

#if DEBUG
		const string staffPing = "{{staffping}}";
#else
        var staffPing = Snowflake.GetMention(() => cfg.StaffRoleID);
#endif

		var announceChannel = Context.Guild.GetTextChannel(cfg.StaffAnnounceChannel);

		if (announceChannel is null)
		{
			Log.Error("Could not find staff announce channel by ID {ChannelID}", cfg.StaffAnnounceChannel.ID);
			return;
		}

		await announceChannel.SendMessageAsync(staffPing, embed: new InstarReportUserEmbed(modal, Context.User, message, guild).Build());

		await metricService.Emit(Metric.ReportUser_ReportsSent, 1);
	}
}