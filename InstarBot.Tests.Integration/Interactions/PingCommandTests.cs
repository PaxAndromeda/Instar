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
        var command = TestUtilities.SetupCommandMock<PingCommand>();

        // Act
        await command.Object.Ping();

        // Assert
        TestUtilities.VerifyMessage(command, "Pong!", true);
    }
}