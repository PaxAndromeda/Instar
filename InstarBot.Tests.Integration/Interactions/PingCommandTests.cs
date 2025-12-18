using InstarBot.Test.Framework;
using PaxAndromeda.Instar.Commands;

namespace InstarBot.Tests.Integration.Interactions;
using Xunit;

public static class PingCommandTests
{
    /// <summary>
    /// Tests that the ping command emits an ephemeral "Pong!" response.
    /// </summary>
    /// <remarks>This test verifies that calling the ping command results in the
    /// expected ephemeral "Pong!" response.</remarks>
    /// <returns></returns>
    [Fact(DisplayName = "User should be able to issue the Ping command.")]
    public static async Task PingCommand_Send_ShouldEmitEphemeralPong()
    {
		// Arrange
		var cmd = TestOrchestrator.Default.GetCommand<PingCommand>();

        // Act
        await cmd.Object.Ping();

		// Assert
		cmd.VerifyResponse("Pong!", true);
    }
}