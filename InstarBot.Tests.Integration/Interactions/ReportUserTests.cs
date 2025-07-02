using Discord;
using FluentAssertions;
using InstarBot.Tests.Services;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Commands;
using PaxAndromeda.Instar.Modals;
using Xunit;

namespace InstarBot.Tests.Integration.Interactions;

public sealed class ReportUserTests
{
    [Fact(DisplayName = "User should be able to report a message normally")]
    public static async Task ReportUser_WhenReportingNormally_ShouldNotifyStaff()
    {
        var context = ReportContext.Builder()
            .FromUser(Snowflake.Generate())
            .Reporting(Snowflake.Generate())
            .InChannel(Snowflake.Generate())
            .WithReason("This is a test report")
            .Build();

        var (command, interactionContext, channelMock) = SetupMocks(context);

        // Act
        await command.Object.HandleCommand(interactionContext.Object);
        await command.Object.ModalResponse(new ReportMessageModal
        {
            ReportReason = context.Reason
        });

        // Assert
        TestUtilities.VerifyMessage(command, "Your report has been sent.", true);

        channelMock.Verify(n => n.SendMessageAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Embed>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<AllowedMentions>(), It.IsAny<MessageReference>(), It.IsAny<MessageComponent>(),
            It.IsAny<ISticker[]>(),
            It.IsAny<Embed[]>(), It.IsAny<MessageFlags>(), It.IsAny<PollProperties>()));

        Assert.NotNull(context.ResultEmbed);
        var embed = context.ResultEmbed;

        embed.Author.Should().NotBeNull();
        embed.Footer.Should().NotBeNull();
        embed.Footer!.Value.Text.Should().Be("Instar Message Reporting System");
    }

    [Fact(DisplayName = "Report user function times out if cache expires")]
    public static async Task ReportUser_WhenReportingNormally_ShouldFail_IfNotCompletedWithin5Minutes()
    {
        var context = ReportContext.Builder()
            .FromUser(Snowflake.Generate())
            .Reporting(Snowflake.Generate())
            .InChannel(Snowflake.Generate())
            .WithReason("This is a test report")
            .Build();

        var (command, interactionContext, _) = SetupMocks(context);

        // Act
        await command.Object.HandleCommand(interactionContext.Object);
        ReportUserCommand.PurgeCache();
        await command.Object.ModalResponse(new ReportMessageModal
        {
            ReportReason = context.Reason
        });

        // Assert
        TestUtilities.VerifyMessage(command, "Report expired.  Please try again.", true);
    }

    private static (Mock<ReportUserCommand>, Mock<IInstarMessageCommandInteraction>, Mock<ITextChannel>) SetupMocks(ReportContext context)
    {
        var commandMockContext = new TestContext
        {
            UserID = context.User,
            EmbedCallback = embed => context.ResultEmbed = embed,
        };

        var commandMock =
            TestUtilities.SetupCommandMock
            (() => new ReportUserCommand(TestUtilities.GetDynamicConfiguration(), new MockMetricService()),
                commandMockContext);

        return (commandMock, SetupMessageCommandMock(context), commandMockContext.TextChannelMock);
    }

    private static Mock<IInstarMessageCommandInteraction> SetupMessageCommandMock(ReportContext context)
    {
        var userMock = TestUtilities.SetupUserMock<IGuildUser>(context.User);
        var authorMock = TestUtilities.SetupUserMock<IGuildUser>(context.Sender);

        var channelMock = TestUtilities.SetupChannelMock<ITextChannel>(context.Channel);

        var messageMock = new Mock<IMessage>();
        messageMock.Setup(n => n.Id).Returns(100);
        messageMock.Setup(n => n.Author).Returns(authorMock.Object);
        messageMock.Setup(n => n.Channel).Returns(channelMock.Object);

        var socketMessageDataMock = new Mock<IMessageCommandInteractionData>();
        socketMessageDataMock.Setup(n => n.Message).Returns(messageMock.Object);

        var socketMessageCommandMock = new Mock<IInstarMessageCommandInteraction>();
        socketMessageCommandMock.Setup(n => n.User).Returns(userMock.Object);
        socketMessageCommandMock.Setup(n => n.Data).Returns(socketMessageDataMock.Object);

        socketMessageCommandMock.Setup<Task>(n =>
                n.RespondWithModalAsync<ReportMessageModal>(It.IsAny<string>(), It.IsAny<RequestOptions>(),
                    It.IsAny<Action<ModalBuilder>>()))
            .Returns(Task.CompletedTask);

        return socketMessageCommandMock;
    }

    private record ReportContext(Snowflake User, Snowflake Sender, Snowflake Channel, string Reason)
    {
        public static ReportContextBuilder Builder()
        {
            return new ReportContextBuilder();
        }

        public Embed? ResultEmbed { get; set; }
    }

    private class ReportContextBuilder
    {
        private Snowflake? _user;
        private Snowflake? _sender;
        private Snowflake? _channel;
        private string? _reason;

        /*
         * ReportContext.Builder()
         *    .FromUser(user)
         *    .Reporting(userToReport)
         *    .WithContent(content)
         *    .InChannel(channel)
         *    .WithReason(reason);
         */
        public ReportContextBuilder FromUser(Snowflake user)
        {
            _user = user;
            return this;
        }

        public ReportContextBuilder Reporting(Snowflake userToReport)
        {
            _sender = userToReport;
            return this;
        }

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
            if (_user is null)
                throw new InvalidOperationException("User must be set before building ReportContext");
            if (_sender is null)
                throw new InvalidOperationException("Sender must be set before building ReportContext");
            if (_channel is null)
                throw new InvalidOperationException("Channel must be set before building ReportContext");
            if (_reason is null)
                throw new InvalidOperationException("Reason must be set before building ReportContext");

            return new ReportContext(_user, _sender, _channel, _reason);
        }
    }
}