using System;
using FluentAssertions;
using Moq;
using PaxAndromeda.Instar;
using Xunit;

namespace InstarBot.Tests;

public class BirthdayTests
{ 
	private TimeProvider GetTimeProvider(DateTimeOffset dateTime)
	{
		var mock = new Mock<TimeProvider>();
		mock.Setup(n => n.GetUtcNow()).Returns(dateTime.UtcDateTime);

		return mock.Object;
	}

	[Theory]
	[InlineData("2025-08-01T00:00:00Z", "1992-07-01T00:00:00Z", 33)] // after birthday
	[InlineData("2025-07-01T00:00:00Z", "1992-07-01T00:00:00Z", 33)] // on birthday
	[InlineData("2025-06-01T00:00:00Z", "1992-07-01T00:00:00Z", 32)] // before birthday
	[InlineData("2025-02-14T00:00:00Z", "1992-02-29T00:00:00Z", 32)] // leap year Birthdate before birthday
	[InlineData("2025-03-01T00:00:00Z", "1992-02-29T00:00:00Z", 33)] // leap year Birthdate after birthday
	public void Age_ShouldReturnExpectedAge(string currentDateStr, string birthDateStr, int expectedAge)
	{
		var timeProvider = GetTimeProvider(DateTime.Parse(currentDateStr));
		var birthDate = DateTime.Parse(birthDateStr).ToUniversalTime();

		var birthday = new Birthday(birthDate, timeProvider);

		birthday.Age.Should().Be(expectedAge);
	}

	[Theory]
	[InlineData("2025-08-01T12:00:00-07:00", "2025-08-01T13:00:00-07:00", true)]
	[InlineData("2025-08-01T12:00:00-07:00", "2025-08-02T13:00:00-07:00", false)]
	[InlineData("2025-08-01T12:00:00Z", "2025-08-01T12:00:00-07:00", true)]
	[InlineData("2025-02-28T12:00:00Z", "2024-02-29T12:00:00Z", true)]
	public void IsToday_ShouldReturnExpected(string currentUtc, string testTime, bool expected)
	{
		var timeProvider = GetTimeProvider(DateTime.Parse(currentUtc));
		var birthDate = DateTime.Parse(testTime).ToUniversalTime();

		var birthday = new Birthday(birthDate, timeProvider);

		birthday.IsToday.Should().Be(expected);
	}

	[Theory]
	[InlineData("2000-07-01T00:00:00Z", "07010000")] 
	[InlineData("2001-07-01T00:00:00Z", "07010000")]
	[InlineData("2000-07-01T00:00:00-08:00", "07010800")]
	[InlineData("2000-08-01T00:00:00Z", "08010000")] 
	[InlineData("2000-02-29T00:00:00Z", "02290000")] 
	public void Key_ShouldReturnExpectedDatabaseKey(string date, string expectedKey)
	{
		var timeProvider = GetTimeProvider(DateTime.Parse(date));
		var birthDate = DateTime.Parse(date).ToUniversalTime();

		var birthday = new Birthday(birthDate, timeProvider);

		birthday.Key.Should().Be(expectedKey);
	}

	[Theory]
	[InlineData("2000-07-01T00:00:00Z", 962409600)] // normal date
	[InlineData("1970-01-01T00:00:00Z", 0)] // epoch
	[InlineData("1969-12-31T23:59:59Z", -1)] // just before epoch
	[InlineData("1900-01-01T00:00:00Z", -2208988800)] // far before epoch
	public void Timestamp_ShouldReturnExpectedTimestamp(string date, long expectedTimestamp)
	{
		var birthDate = DateTime.Parse(date).ToUniversalTime();

		var birthday = new Birthday(birthDate, TimeProvider.System);

		birthday.Timestamp.Should().Be(expectedTimestamp);
	}

	[Theory]
	[InlineData("2025-06-15T00:00:00Z", "1990-07-01T00:00:00Z", "2025-07-01T00:00:00Z")] // birthday later in year
	[InlineData("2025-08-15T00:00:00Z", "1990-07-01T00:00:00Z", "2025-07-01T00:00:00Z")] // birthday earlier in year
	[InlineData("2024-02-15T00:00:00Z", "1992-02-29T00:00:00Z", "2024-02-29T00:00:00Z")] // leap year birthday on leap year
	[InlineData("2025-02-15T00:00:00Z", "1992-02-29T00:00:00Z", "2025-02-28T00:00:00Z")] // leap year birthday on non-leap year
	public void Observed_ShouldReturnThisYearsBirthday(string currentTime, string birthdate, string expectedObservedDate)
	{
		var timeProvider = GetTimeProvider(DateTime.Parse(currentTime));
		var birthday = new Birthday(DateTimeOffset.Parse(birthdate), timeProvider);

		var expectedTime = DateTimeOffset.Parse(expectedObservedDate);

		birthday.Observed.Should().Be(expectedTime);
	}

	[Theory]
	[InlineData("2025-06-15T00:00:00Z", "1990-07-01T00:00:00Z", "2025-07-01T00:00:00Z")] // birthday later in year
	[InlineData("2025-08-15T00:00:00Z", "1990-07-01T00:00:00Z", "2026-07-01T00:00:00Z")] // birthday earlier in year
	[InlineData("2024-02-15T00:00:00Z", "1992-02-29T00:00:00Z", "2024-02-29T00:00:00Z")] // leap year birthday on leap year
	[InlineData("2025-02-15T00:00:00Z", "1992-02-29T00:00:00Z", "2025-02-28T00:00:00Z")] // leap year birthday on non-leap year
	public void Next_ShouldReturnThisYearsBirthday(string currentTime, string birthdate, string expectedObservedDate)
	{
		var timeProvider = GetTimeProvider(DateTime.Parse(currentTime));
		var birthday = new Birthday(DateTimeOffset.Parse(birthdate), timeProvider);

		var expectedTime = DateTimeOffset.Parse(expectedObservedDate);

		birthday.Next.Should().Be(expectedTime);
	}
}