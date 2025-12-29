using System;
using FluentAssertions;
using PaxAndromeda.Instar;
using Xunit;

namespace InstarBot.Tests;

public sealed class SnowflakeTests
{
    // This snowflake has a time of 2016-04-30 11:18:25.769 UTC, and a generated ID of 7
    private const ulong DefaultTestSnowflake = 175928847299117063;

    private static Snowflake UnknownSnowflake => new(DefaultTestSnowflake);

    [SnowflakeType(SnowflakeType.User)] private static Snowflake UserSnowflake => new(DefaultTestSnowflake);

    [SnowflakeType(SnowflakeType.Role)] private static Snowflake RoleSnowflake => new(DefaultTestSnowflake);

    [SnowflakeType(SnowflakeType.Channel)] private static Snowflake ChannelSnowflake => new(DefaultTestSnowflake);

    [SnowflakeType(SnowflakeType.Guild)] private static Snowflake GuildSnowflake => new(DefaultTestSnowflake);

    private static Snowflake SnowflakeReturnMethod()
    {
        return UnknownSnowflake;
    }

    private static ulong GenerateSnowflakeFromTimestamp(DateTime time)
    {
        const long discordEpoch = 1420070400000;

        if (time.Kind != DateTimeKind.Utc)
            time = time.ToUniversalTime();

        var epoch = new DateTime(1970, 1, 1);
        var ms = (long)(time - epoch).TotalMilliseconds;

        return (ulong)((ms - discordEpoch) << 22);
    }

    [Fact]
    public void WithIndividualParameters_ShouldReturnOK()
    {
        // Arrange
        const int internalWorkerId = 1;
        const int internalProcessId = 0;
        const int generatedId = 7;
        var time = new DateTime(2016, 4, 30, 11, 18, 25, 796, DateTimeKind.Utc);

        // Act
        var snowflake = new Snowflake(time, internalWorkerId, internalProcessId, generatedId);

        // Assert
        snowflake.ID.Should().Be(DefaultTestSnowflake);
        snowflake.Time.Should().Be(time);
        snowflake.InternalWorkerId.Should().Be(internalWorkerId);
        snowflake.InternalProcessId.Should().Be(internalProcessId);
        snowflake.GeneratedId.Should().Be(generatedId);
    }

    [Fact]
    public void WithDateTimeOnly_ShouldReturnOK()
    {
        // Arrange
        var time = new DateTime(2016, 4, 30, 11, 18, 25, 796, DateTimeKind.Utc);
        var expectedSnowflake = GenerateSnowflakeFromTimestamp(time);

        // Act
        var snowflake = new Snowflake(time);

        // Assert
        snowflake.ID.Should().Be(expectedSnowflake);
        snowflake.Time.Should().Be(time);
        snowflake.InternalWorkerId.Should().Be(0);
        snowflake.InternalProcessId.Should().Be(0);
        snowflake.GeneratedId.Should().Be(0);
    }

    [Fact]
    public void ValidSnowflake_ShouldReturnOK()
    {
        // Arrange
        var expectedTime = new DateTime(2016, 4, 30, 11, 18, 25, 796, DateTimeKind.Utc);

        // Act
        var snowflake = new Snowflake(DefaultTestSnowflake);

        // Assert
        snowflake.ID.Should().Be(DefaultTestSnowflake);
        snowflake.Time.Should().Be(expectedTime);
        snowflake.InternalWorkerId.Should().Be(1);
        snowflake.InternalProcessId.Should().Be(0);
        snowflake.GeneratedId.Should().Be(7);
    }

    [Fact]
    public void SnowflakeTooEarly_ShouldThrowArgumentException()
    {
        // Arrange
        var discordSnowflake =
            GenerateSnowflakeFromTimestamp(new DateTime(2014, 4, 30, 11, 18, 25, 796, DateTimeKind.Utc));

        // Act
        var act = () => new Snowflake(discordSnowflake);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TimeTooEarly_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var time = new DateTime(2014, 4, 30, 11, 18, 25, 796);

        // Act
        var act = () => new Snowflake(time);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void InvalidWorkerID_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var time = new DateTime(2016, 4, 30, 11, 18, 25, 796);

        // Act
        var act = () => new Snowflake(time, -1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void InvalidProcessID_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var time = new DateTime(2016, 4, 30, 11, 18, 25, 796);

        // Act
        var act = () => new Snowflake(time, 0, -1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void InvalidGeneratedID_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var time = new DateTime(2016, 4, 30, 11, 18, 25, 796);

        // Act
        var act = () => new Snowflake(time, 0, 0, -1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ImplicitOperatorTests_ShouldReturnProperValues()
    {
        // Arrange
        const int internalWorkerId = 1;
        const int internalProcessId = 0;
        const int generatedId = 7;
        var time = new DateTime(2016, 4, 30, 11, 18, 25, 796, DateTimeKind.Utc);

        // Act
        var snowflake = new Snowflake(time, internalWorkerId, internalProcessId, generatedId);

        // Assert
        ulong id = snowflake;
        DateTime newDateTime = snowflake;

        id.Should().Be(DefaultTestSnowflake);
        newDateTime.Should().Be(time);
    }

    [Fact]
    public void Generate_ShouldBeUnique()
    {
        // Act
        var snowflake1 = Snowflake.Generate();
        var snowflake2 = Snowflake.Generate();

		// Assert
		snowflake2.GeneratedId.Should().BeGreaterThan(snowflake1.GeneratedId);
        snowflake1.Should().NotBe(snowflake2);
    }

    [Fact]
    public void GetMention_User_ShouldReturnUserMention()
    {
        var mention = Snowflake.GetMention(() => UserSnowflake);

        mention.Should().Be($"<@{DefaultTestSnowflake}>");
    }

    [Fact]
    public void GetMention_Role_ShouldReturnRoleMention()
    {
        var mention = Snowflake.GetMention(() => RoleSnowflake);

        mention.Should().Be($"<@&{DefaultTestSnowflake}>");
    }

    [Fact]
    public void GetMention_Channel_ShouldReturnChannelMention()
    {
        var mention = Snowflake.GetMention(() => ChannelSnowflake);

        mention.Should().Be($"<#{DefaultTestSnowflake}>");
    }

    [Fact]
    public void GetMention_Guild_ShouldThrowException()
    {
        var func = () => Snowflake.GetMention(() => GuildSnowflake);

        func.Should().ThrowExactly<InvalidOperationException>().WithMessage("Cannot mention ID type Guild");
    }

    [Fact]
    public void GetMention_PropertyWithNoAttribute_ShouldThrowException()
    {
        var func = () => Snowflake.GetMention(() => UnknownSnowflake);

        func.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("Cannot get the mention for a member not containing a SnowflakeType attribute");
    }

    [Fact]
    public void GetMention_SelectsWrongThing_ShouldThrowException()
    {
        var func = () => Snowflake.GetMention(() => SnowflakeReturnMethod());

        func.Should().ThrowExactly<ArgumentException>();
    }
}