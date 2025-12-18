using Discord;
using InstarBot.Tests;
using Moq;
using Moq.Protected;
using PaxAndromeda.Instar.Commands;

namespace InstarBot.Test.Framework;

public static class MockExtensions
{
	extension<T>(Mock<T> channel) where T : class, IMessageChannel
	{
		/// <summary>
		/// Verifies that the command responded to the user with the correct <paramref name="format"/>.
		/// </summary>
		/// <param name="format">The string format to check called messages against.</param>
		/// <param name="partial">A flag indicating whether partial matches are acceptable.</param>
		public void VerifyMessage(string format, bool partial = false)
		{
			channel.Verify(c => c.SendMessageAsync(
					It.Is<string>(s => TestUtilities.MatchesFormat(s, format, partial)),
					false,
					It.IsAny<Embed>(),
					It.IsAny<RequestOptions>(),
					It.IsAny<AllowedMentions>(),
					It.IsAny<MessageReference>(),
					It.IsAny<MessageComponent>(),
					It.IsAny<ISticker[]>(),
					It.IsAny<Embed[]>(),
					It.IsAny<MessageFlags>(),
					It.IsAny<PollProperties>()
				));
		}
	}

	extension<T>(Mock<T> channel) where T : class, ITextChannel
	{
		/// <summary>
		/// Verifies that the command responded to the user with an embed that satisfies the specified <paramref name="verifier"/>.
		/// </summary>
		/// <typeparam name="T">The type of command. Must implement <see cref="InteractionModuleBase&lt;T&gt;"/>.</typeparam>
		/// <param name="verifier">An <see cref="EmbedVerifier"/> instance to verify against.</param>
		/// <param name="format">An optional message format, if present. Defaults to null.</param>
		/// <param name="partial">An optional flag indicating whether partial matches are acceptable. Defaults to false.</param>
		public void VerifyMessageEmbed(EmbedVerifier verifier, string format, bool partial = false)
		{
			channel.Verify(c => c.SendMessageAsync(
					It.Is<string>(n => TestUtilities.MatchesFormat(n, format, partial)),
					false,
					It.Is<Embed>(e => verifier.Verify(e)),
					It.IsAny<RequestOptions>(),
					It.IsAny<AllowedMentions>(),
					It.IsAny<MessageReference>(),
					It.IsAny<MessageComponent>(),
					It.IsAny<ISticker[]>(),
					It.IsAny<Embed[]>(),
					It.IsAny<MessageFlags>(),
					It.IsAny<PollProperties>()
				));
		}
	}

	extension<T>(Mock<T> command) where T : BaseCommand
	{
		public void VerifyResponse(string format, bool ephemeral = false, bool partial = false)
			=> command.VerifyResponseAndEmbed(format, null, ephemeral, partial);

		public void VerifyResponse(EmbedVerifier embedVerifier, bool ephemeral = false, bool partial = false)
			=> command.VerifyResponseAndEmbed(null, embedVerifier, ephemeral, partial);

		public void VerifyResponse(string format, EmbedVerifier embedVerifier, bool ephemeral = false, bool partial = false)
			=> command.VerifyResponseAndEmbed(format, embedVerifier, ephemeral, partial);

		private void VerifyResponseAndEmbed(string? format = null, EmbedVerifier? embedVerifier = null, bool ephemeral = false, bool partial = false)
		{
			var msgRef = format is null
				? ItExpr.IsNull<string>()
				: ItExpr.Is<string>(n => TestUtilities.MatchesFormat(n, format, partial));

			var embedRef = embedVerifier is null
				? ItExpr.IsAny<Embed>()
				: ItExpr.Is<Embed>(e => embedVerifier.Verify(e));

			command.Protected().Verify(
				"RespondAsync",
				Times.Once(),
				msgRef,								// text
				ItExpr.IsAny<Embed[]>(),            // embeds
				false,                              // isTTS
				ephemeral,                          // ephemeral
				ItExpr.IsAny<AllowedMentions>(),    // allowedMentions
				ItExpr.IsAny<RequestOptions>(),     // options
				ItExpr.IsAny<MessageComponent>(),   // components
				embedRef,							// embed
				ItExpr.IsAny<PollProperties>(),     // pollProperties
				ItExpr.IsAny<MessageFlags>()        // messageFlags
			);
		}
	}
}