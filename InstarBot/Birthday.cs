using System.Diagnostics.CodeAnalysis;

namespace PaxAndromeda.Instar;

/// <summary>
/// Represents a person's birthday, providing methods to calculate the observed birthday, age, and related information
/// based on a specified date and time provider.
/// </summary>
/// <param name="Birthdate">The date and time of birth, including the time zone offset.</param>
/// <param name="TimeProvider">An object that supplies the current date and time. If not specified, the system time provider is used.</param>
public record Birthday(DateTimeOffset Birthdate, TimeProvider TimeProvider)
{
	/// <summary>
	/// Gets the date of the next observed birthday based on the current year.
	/// </summary>
	/// <remarks>
	///	For most scenarios, this is the date in the current year with the same month and day
	/// as the user's Birthdate. However, for users born on February 29th, this will be defined
	/// as the day immediately preceding March 1st in non-leap years (i.e., February 28th).
	/// </remarks>
	public DateTimeOffset Observed
	{
		get
		{
			var birthdayNormalized = Normalize(Birthdate);
			var now = TimeProvider.GetUtcNow().ToOffset(birthdayNormalized.Offset);

			return new DateTimeOffset(now.Year, birthdayNormalized.Month, birthdayNormalized.Day,
				birthdayNormalized.Hour, birthdayNormalized.Minute, birthdayNormalized.Second,
				birthdayNormalized.Offset);
		}
	}

	/// <summary>
	/// Gets the date of the next birthday.
	/// </summary>
	/// <remarks>
	///	Internally this just returns <see cref="Observed"/>, but will add a year if the
	/// value is in the past.  As such, <see cref="Next"/> is always in the future.
	/// </remarks>
	public DateTimeOffset Next
	{
		get
		{
			var observed = Observed;
			var now = TimeProvider.GetUtcNow().ToOffset(Observed.Offset);

			if (observed < now)
				observed = observed.AddYears(1);

			return observed;
		}
	}

	/// <summary>
	/// Gets the age, in years, calculated from the Birthdate to the current date.
	/// </summary>
	/// <remarks>
	/// To accurately determine the age of users born on February 29th, this calculation
	/// will use the normalized Birthdate for the given year, which will be defined as
	/// the day immediately preceding March 1st in non-leap years (i.e., February 28th).
	/// </remarks>
	public int Age
	{
		get
		{
			var now = TimeProvider.GetUtcNow().ToOffset(Birthdate.Offset);

			// Preliminary age based on year difference
			int age = now.Year - Birthdate.Year;

			// If the test date is before the anniversary this year, they haven't had their birthday yet
			if (now < Observed)
				age--;

			return age;
		}
	}

	/// <summary>
	/// Gets a value indicating whether the observed date and time occur on the current day in the observed time zone.
	/// </summary>
	/// <remarks>This property compares the date portion of the observed time with the current date, adjusted to the
	/// same time zone offset as the observed value. It returns <see langword="true"/> if the observed time falls within
	/// the range of the current local day; otherwise, <see langword="false"/>.</remarks>
	public bool IsToday
	{
		get
		{
			var dtNow = TimeProvider.GetUtcNow();

			// Convert current UTC time to the same offset as localTime
			var utcOffset = Observed.Offset;
			var currentLocalTime = dtNow.ToOffset(utcOffset);

			var localTimeToday = new DateTimeOffset(currentLocalTime.Date, currentLocalTime.Offset);
			var localTimeTomorrow = localTimeToday.Date.AddDays(1);

			return Observed >= localTimeToday && Observed < localTimeTomorrow;
		}
	}

	/// <summary>
	/// Returns a string key representing the specified Birthdate in UTC, formatted as month, day, hour, and minute.
	/// </summary>
	/// <remarks>This method is useful for creating compact, sortable keys based on Birthdate and time. The returned
	/// string uses UTC time to ensure consistency across time zones.</remarks>
	public string Key => Utilities.ToBirthdateKey(Birthdate);

	/// <summary>
	/// Gets the Unix timestamp representing the number of seconds that have elapsed since 00:00:00 UTC on 1 January 1970
	/// (the Unix epoch) for the associated Birthdate.
	/// </summary>
	public long Timestamp => Birthdate.ToUnixTimeSeconds();

	[ExcludeFromCodeCoverage]
	public Birthday(int year, int month, int day, int hour, int minute, int second, DateTimeKind kind, TimeProvider provider)
		: this(new DateTimeOffset(new DateTime(year, month, day, hour, minute, second, kind)), provider)
	{ }

	[ExcludeFromCodeCoverage]
	public Birthday(int year, int month, int day, int hour, int minute, int second, TimeSpan offset, TimeProvider provider)
		: this(new DateTimeOffset(year, month, day, hour, minute, second, offset), provider)
	{ }

	private DateTimeOffset Normalize(DateTimeOffset dto)
	{
		// Return as is if the year is a leap year
		if (DateTime.IsLeapYear(TimeProvider.GetUtcNow().ToOffset(Birthdate.Offset).Year))
			return dto;

		return dto is not { Month: 2, Day: 29 }
			// Return as is if the date is not Feb 29
			? dto

			// Default to Feb 28 on non-leap years. Sorry Feb 29 babies.
			: new DateTimeOffset(dto.Year, 2, 28, dto.Hour, dto.Minute, dto.Second, dto.Offset);
	}
}