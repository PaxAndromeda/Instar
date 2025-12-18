using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using JetBrains.Annotations;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Embeds;
using PaxAndromeda.Instar.Metrics;
using PaxAndromeda.Instar.Services;
using Serilog;

namespace PaxAndromeda.Instar.Commands;

// Required to be unsealed for mocking
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
public class SetBirthdayCommand(IDatabaseService ddbService, IDynamicConfigService dynamicConfig, IMetricService metricService, IBirthdaySystem birthdaySystem, TimeProvider timeProvider) : BaseCommand
{
	/// <summary>
	/// The default year to use when none is provided. We select a year that is sufficiently
	/// far in the past to be obviously unset, as well as a leap year to accommodate February 29th birthdays.
	///
	/// DISCLAIMER: We are not actually asserting that a user that does not provide a year is 425 years old.
	/// </summary>
	private const int DefaultYear = 1600;

	[UsedImplicitly]
	[SlashCommand("setbirthday", "Sets your birthday on the server.")]
	public async Task SetBirthday(
		[MinValue(1)] [MaxValue(12)] [Summary(description: "The month you were born.")]
		Month month,
		[MinValue(1)] [MaxValue(31)] [Summary(description: "The day you were born.")]
		int day,
		[MinValue(1900)] [MaxValue(2099)] [Summary(description: "The year you were born. We use this to automatically update age roles.")]
		int? year = null,
		[MinValue(-12)]
		[MaxValue(+12)]
		[Summary("timezone", "Set your time zone so your birthday role is applied at the correct time of day.")]
		[Autocomplete]
		int tzOffset = 0)
	{
		if (Context.User is null)
		{
			Log.Warning("Context.User was null");
			await RespondAsync(
				Strings.Command_SetBirthday_Error_Unknown,
				ephemeral: true);
			return;
		}

		if ((int) month is < 0 or > 12)
		{
			await RespondAsync(
				Strings.Command_SetBirthday_MonthsOutOfRange,
				ephemeral: true);
			return;
		}

		// We have to assume a leap year if the user did not provide a year.
		int actualYear = year ?? DefaultYear;

		var daysInMonth = DateTime.DaysInMonth(actualYear, (int)month);

		// First step:  Does the provided number of days exceed the number of days in the given month?
		if (day > daysInMonth)
		{
			await RespondAsync(
				string.Format(Strings.Command_SetBirthday_DaysInMonthOutOfRange, daysInMonth, month, year),
				ephemeral: true);
			return;
		}

		var unspecifiedDate = new DateTime(actualYear, (int)month, day, 0, 0, 0, DateTimeKind.Unspecified);
		var dtZ = new DateTimeOffset(unspecifiedDate, TimeSpan.FromHours(tzOffset));
		
		// Second step:  Is the provided birthday actually in the future?
		if (dtZ.UtcDateTime > timeProvider.GetUtcNow())
		{
			await RespondAsync(
				Strings.Command_SetBirthday_NotTimeTraveler,
				ephemeral: true);
			return;
		}
		
		var birthday = new Birthday(dtZ, timeProvider);

		

		// Third step:  Is the user below the age of 13?
		var cfg = await dynamicConfig.GetConfig();
		bool isUnderage = birthday.Age < cfg.BirthdayConfig.MinimumPermissibleAge;

        try
        {
            var dbUser = await ddbService.GetOrCreateUserAsync(Context.User);
			if (dbUser.Data.Birthday is not null)
			{
				var originalBirthdayTimestamp = dbUser.Data.Birthday.Timestamp;
				await RespondAsync(string.Format(Strings.Command_SetBirthday_Error_AlreadySet, originalBirthdayTimestamp), ephemeral: true);
				return;
			}

			if (isUnderage)
			{
				Log.Warning("User {UserID} recorded a birthday that puts their age below 13!  {UtcTime}", Context.User!.Id,
					birthday.Birthdate.UtcDateTime);

				await HandleUnderage(cfg, Context.User, birthday);
			}

			dbUser.Data.Birthday = birthday;
			dbUser.Data.Birthdate = birthday.Key;

			// If the user is underage and is a new member and does not already have an auto member hold record,
			// we automatically withhold their membership for staff review.
			if (isUnderage && dbUser.Data is { Position: InstarUserPosition.NewMember, AutoMemberHoldRecord: null })
			{
				dbUser.Data.AutoMemberHoldRecord = new AutoMemberHoldRecord
				{
					Date = timeProvider.GetUtcNow().UtcDateTime,
					ModeratorID = cfg.BotUserID,
					Reason = string.Format(Strings.Command_SetBirthday_Underage_AMHReason, birthday.Timestamp, cfg.BirthdayConfig.MinimumPermissibleAge)
				};
			}

			await dbUser.CommitAsync();
			
            Log.Information("User {UserID} birthday set to {DateTime} (UTC time calculated as {UtcTime})",
                Context.User!.Id,
                birthday.Birthdate, birthday.Birthdate.UtcDateTime);

			// Fourth step: Grant birthday role if the user's birthday is today THEIR time.
			// TODO: Ensure that a user is granted/removed birthday roles appropriately after setting their birthday IF their birthday is today.
			if (birthday.IsToday)
			{
				// User's birthday is today in their timezone; grant birthday role.
				await birthdaySystem.GrantUnexpectedBirthday(Context.User, birthday);
			}

			await RespondAsync(string.Format(Strings.Command_SetBirthday_Success, birthday.Timestamp), ephemeral: true);
            await metricService.Emit(Metric.BS_BirthdaysSet, 1);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update {UserID}'s birthday due to a DynamoDB failure",
                Context.User!.Id);

            await RespondAsync(Strings.Command_SetBirthday_Error_CouldNotSetBirthday,
                ephemeral: true);
        }
    }

	private async Task HandleUnderage(InstarDynamicConfiguration cfg, IGuildUser user, Birthday birthday)
	{
		var staffAnnounceChannel = Context.Guild.GetTextChannel(cfg.StaffAnnounceChannel);

		var warningEmbed = new InstarUnderageUserWarningEmbed(cfg, user, user.RoleIds.Contains(cfg.MemberRoleID), birthday).Build();

		await staffAnnounceChannel.SendMessageAsync($"<@&{cfg.StaffRoleID}>", embed: warningEmbed);
	}

	[UsedImplicitly]
    [ExcludeFromCodeCoverage(Justification = "No logic.  Just returns list")]
    [AutocompleteCommand("timezone", "setbirthday")]
    public async Task HandleTimezoneAutocomplete()
    {
        Log.Debug("AUTOCOMPLETE");
        IEnumerable<AutocompleteResult> results =
        [
            new("GMT-12 International Date Line West", -12),
            new("GMT-11 Midway Island, Samoa", -11),
            new("GMT-10 Hawaii", -10),
            new("GMT-9 Alaska, R'lyeh", -9),
            new("GMT-8 Pacific Time (US and Canada); Tijuana", -8),
            new("GMT-7 Mountain Time (US and Canada)", -7),
            new("GMT-6 Central Time (US and Canada)", -6),
            new("GMT-5 Eastern Time (US and Canada)", -5),
            new("GMT-4 Atlantic Time (Canada)", -4),
            new("GMT-3 Brasilia, Buenos Aires, Georgetown", -3),
            new("GMT-2 Mid-Atlantic", -2),
            new("GMT-1 Azores, Cape Verde Islands", -1),
            new("GMT+0 Greenwich Mean Time: Dublin, Edinburgh, Lisbon, London", 0),
            new("GMT+1 Amsterdam, Berlin, Bern, Rome, Stockholm, Vienna", 1),
            new("GMT+2 Helsinki, Kiev, Riga, Sofia, Tallinn, Vilnius", 2),
            new("GMT+3 Moscow, St. Petersburg, Volgograd", 3),
            new("GMT+4 Abu Dhabi, Muscat", 4),
            new("GMT+5 Islamabad, Karachi, Tashkent", 5),
            new("GMT+6 Astana, Dhaka", 6),
            new("GMT+7 Bangkok, Hanoi, Jakarta", 7),
            new("GMT+8 Beijing, Chongqing, Hong Kong SAR, Urumqi", 8),
            new("GMT+9 Seoul, Osaka, Sapporo, Tokyo", 9),
            new("GMT+10 Canberra, Melbourne, Sydney", 10),
            new("GMT+11 Magadan, Solomon Islands, New Caledonia", 11),
            new("GMT+12 Auckland, Wellington", 12)
        ];

        await (Context.Interaction as SocketAutocompleteInteraction)?.RespondAsync(results)!;
    }
}