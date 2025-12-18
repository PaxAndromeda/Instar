using Discord;
using InstarBot.Test.Framework.Models;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.DynamoModels;

namespace InstarBot.Test.Framework;

public class TestDatabaseContext
{
}

public class TestDatabaseContextBuilder
{
	private readonly TestDiscordContextBuilder? _discordContextBuilder;

	private Dictionary<Snowflake, InstarUserData> _registeredUsers = new();

	public TestDatabaseContextBuilder(ref TestDiscordContextBuilder? discordContextBuilder)
	{
		_discordContextBuilder = discordContextBuilder;
	}

	public TestDatabaseContextBuilder RegisterUser(Snowflake userId)
		=> RegisterUser(userId, x => x);

	public TestDatabaseContextBuilder RegisterUser(Snowflake userId, Func<InstarUserData, InstarUserData> editExpr)
	{
		if (_registeredUsers.TryGetValue(userId, out InstarUserData? userData))
		{
			_registeredUsers[userId] = editExpr(userData);
			return this;
		}

		if (_discordContextBuilder is null || !_discordContextBuilder.TryGetUser(userId, out TestGuildUser guildUser))
			throw new InvalidOperationException($"You must register {userId.ID} as a Discord user before calling this method.");

		_registeredUsers[userId] = editExpr(InstarUserData.CreateFrom(guildUser));

		return this;
	}
}