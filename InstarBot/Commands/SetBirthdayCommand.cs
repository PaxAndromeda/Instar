using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using JetBrains.Annotations;
using PaxAndromeda.Instar.Metrics;
using PaxAndromeda.Instar.Services;
using Serilog;

namespace PaxAndromeda.Instar.Commands;

// Required to be unsealed for mocking
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global")]
public class SetBirthdayCommand(IInstarDDBService ddbService, IMetricService metricService) : BaseCommand
{
    [UsedImplicitly]
    [RequireOwner]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [SlashCommand("setbirthday", "Sets your birthday on the server.")]
    public async Task SetBirthday(
        [MinValue(1)] [MaxValue(12)] [Summary(description: "The month you were born.")]
        Month month,
        [MinValue(1)] [MaxValue(31)] [Summary(description: "The day you were born.")]
        int day,
        [MinValue(1900)] [MaxValue(2099)] [Summary(description: "The year you were born.")]
        int year,
        [MinValue(-12)]
        [MaxValue(+12)]
        [Summary("timezone", "Select your nearest time zone offset in hours from GMT.")]
        [Autocomplete]
        int tzOffset = 0)
    {
        var daysInMonth = DateTime.DaysInMonth(year, (int)month);

        // First step:  Does the provided number of days exceed the number of days in the given month?
        if (day > daysInMonth)
        {
            await RespondAsync(
                $"There are only {daysInMonth} days in {month} {year}.  Your birthday was not set.",
                ephemeral: true);
            return;
        }

        var dtLocal = new DateTime(year, (int)month, day, 0, 0, 0, DateTimeKind.Unspecified);
        var dtUtc = new DateTime(year, (int)month, day, 0, 0, 0, DateTimeKind.Utc).AddHours(-tzOffset);

        // Second step:  Is the provided birthday actually in the future?
        if (dtUtc > DateTime.UtcNow)
        {
            await RespondAsync(
                "You are not a time traveler.  Your birthday was not set.",
                ephemeral: true);
            return;
        }

        // Third step:  Is the user below the age of 13?
        // Note:  We will assume all years are 365.25 days to account for leap year madness.
        if (DateTime.UtcNow - dtUtc < TimeSpan.FromDays(365.25 * 13))
            Log.Warning("User {UserID} recorded a birthday that puts their age below 13!  {UtcTime}", Context.User!.Id,
                dtUtc);
        // TODO:  Notify staff?

        var ok = await ddbService.UpdateUserBirthday(new Snowflake(Context.User!.Id), dtUtc);

        if (ok)
        {
            Log.Information("User {UserID} birthday set to {DateTime} (UTC time calculated as {UtcTime})",
                Context.User!.Id,
                dtLocal, dtUtc);

            await RespondAsync($"Your birthday was set to {dtLocal:D}.", ephemeral: true);
            await metricService.Emit(Metric.BS_BirthdaysSet, 1);
        }
        else
        {
            Log.Warning("Failed to update {UserID}'s birthday due to a DynamoDB failure",
                Context.User!.Id);

            await RespondAsync("Your birthday could not be set at this time.  Please try again later.",
                ephemeral: true);
        }
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
            new("GMT-9 Alaska", -9),
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