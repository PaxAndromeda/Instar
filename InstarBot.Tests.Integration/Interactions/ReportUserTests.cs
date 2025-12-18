using Discord;
using FluentAssertions;
using InstarBot.Test.Framework;
using InstarBot.Test.Framework.Models;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.Modals;
using Xunit;

namespace InstarBot.Tests.Integration.Interactions;

public static class ReportUserTests
{
	private static async Task<TestOrchestrator> SetupOrchestrator(ReportContext context)
	{
		var orchestrator = TestOrchestrator.Default;

		orchestrator.CreateChannel(context.Channel);

		return orchestrator;
	}

    [Fact(DisplayName = "User should be able to report a message normally")]
    public static async Task ReportUser_WhenReportingNormally_ShouldNotifyStaff()
    {
        var context = ReportContext.Builder()
            .InChannel(Snowflake.Generate())
            .WithReason("This is a test report")
            .Build();

		var orchestrator = await SetupOrchestrator(context);
		var command = orchestrator.GetCommand<ReportUserCommand>();

		var verifier = EmbedVerifier.Builder()
			.WithFooterText(Strings.Embed_UserReport_Footer)
			.WithField("Reason", $"```{context.Reason}```")
			.WithField("Message Content", "{0}")
			.WithField("User", "<@{0}>")
			.WithField("Channel", "<#{0}>")
			.WithField("Reported By", "<@{0}>")
			.Build();

        // Act
        await command.Object.HandleCommand(SetupMessageCommandMock(orchestrator, context));
        await command.Object.ModalResponse(new ReportMessageModal
        {
            ReportReason = context.Reason
        });

		// Assert
		command.VerifyResponse(Strings.Command_ReportUser_ReportSent, true);
		
		((TestChannel) await orchestrator.Discord.GetChannel(orchestrator.Configuration.StaffAnnounceChannel)).VerifyEmbed(verifier, "{@}");
    }

    [Fact(DisplayName = "Report user function times out if cache expires")]
    public static async Task ReportUser_WhenReportingNormally_ShouldFail_IfNotCompletedWithin5Minutes()
    {
        var context = ReportContext.Builder()
            .InChannel(Snowflake.Generate())
            .WithReason("This is a test report")
            .Build();

		var orchestrator = await SetupOrchestrator(context);
		var command = orchestrator.GetCommand<ReportUserCommand>();

		// Act
		await command.Object.HandleCommand(SetupMessageCommandMock(orchestrator, context));
        ReportUserCommand.PurgeCache();
        await command.Object.ModalResponse(new ReportMessageModal
        {
            ReportReason = context.Reason
        });

		// Assert
		command.VerifyResponse(Strings.Command_ReportUser_ReportExpired, true);
    }

    private static IInstarMessageCommandInteraction SetupMessageCommandMock(TestOrchestrator orchestrator, ReportContext context)
    {
		TestChannel testChannel = (TestChannel) orchestrator.Guild.GetTextChannel(context.Channel);
		var message = testChannel.AddMessage(orchestrator.Subject, "Naughty message");

		var socketMessageDataMock = new Mock<IMessageCommandInteractionData>();
        socketMessageDataMock.Setup(n => n.Message).Returns(message);

        var socketMessageCommandMock = new Mock<IInstarMessageCommandInteraction>();
        socketMessageCommandMock.Setup(n => n.User).Returns(orchestrator.Actor);
        socketMessageCommandMock.Setup(n => n.Data).Returns(socketMessageDataMock.Object);

        socketMessageCommandMock.Setup<Task>(n =>
                n.RespondWithModalAsync<ReportMessageModal>(It.IsAny<string>(), It.IsAny<RequestOptions>(),
                    It.IsAny<Action<ModalBuilder>>()))
            .Returns(Task.CompletedTask);
        
        return socketMessageCommandMock.Object;
    }

    private record ReportContext(Snowflake Channel, string Reason)
    {
        public static ReportContextBuilder Builder()
        {
            return new ReportContextBuilder();
        }

        public Embed? ResultEmbed { get; set; }
    }

    private class ReportContextBuilder
    {
        private Snowflake? _channel;
        private string? _reason;

        public ReportContextBuilder InChannel(Snowflake channel)
        {
            _channel = channel;
            return this;
        }

        public ReportContextBuilder WithReason(string reason)
        {
            _reason = reason;
            return this;
        }

        public ReportContext Build()
        {
            if (_channel is null)
                throw new InvalidOperationException("Channel must be set before building ReportContext");
            if (_reason is null)
                throw new InvalidOperationException("Reason must be set before building ReportContext");

            return new ReportContext(_channel, _reason);
        }
    }
}