using FluentAssertions;
using InstarBot.Test.Framework;
using InstarBot.Test.Framework.Models;
using InstarBot.Test.Framework.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Gaius;
using PaxAndromeda.Instar.Modals;
using PaxAndromeda.Instar.Services;
using Xunit;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace InstarBot.Tests.Integration.Services;

public static class AutoMemberSystemTests
{
    private static readonly Snowflake NewMember = new(796052052433698817);
    private static readonly Snowflake Member = new(793611808372031499);
    private static readonly Snowflake Transfemme = new(796085775199502357);
    private static readonly Snowflake TwentyOnePlus = new(796148869855576064);
    private static readonly Snowflake SheHer = new(796578609535647765);
    private static readonly Snowflake AutoMemberHold = new(966434762032054282);

	private static async Task<TestOrchestrator> SetupOrchestrator(AutoMemberSystemContext context)
	{
		var orchestrator = await TestOrchestrator.Builder
			.WithSubject(new TestGuildUser
			{
				Username = "username",
				JoinedAt = DateTimeOffset.Now - TimeSpan.FromHours(context.HoursSinceJoined),
				RoleIds = context.Roles.Select(n => n.ID).ToList().AsReadOnly()
			})
			.WithService<IAutoMemberSystem, AutoMemberSystem>()
			.Build();

		await orchestrator.Subject.AddRoleAsync(orchestrator.Configuration.NewMemberRoleID);
		var channel = orchestrator.CreateChannel(Snowflake.Generate());

		if (context.PostedIntroduction)
		{
			TestChannel introChannel = (TestChannel) await orchestrator.Discord.GetChannel(orchestrator.Configuration.AutoMemberConfig.IntroductionChannel);
			introChannel.AddMessage(orchestrator.Subject, "Some introduction");
		}
		
		for (var i = 0; i < context.MessagesLast24Hours; i++)
			channel.AddMessage(orchestrator.Subject, "Some text");

		if (context.GrantedMembershipBefore)
		{
			var dbUser = await orchestrator.Database.GetUserAsync(orchestrator.Subject.Id);
			dbUser.Data.Position = InstarUserPosition.Member;
			await dbUser.CommitAsync();
		}

		if (context.GaiusInhibited)
		{
			TestGaiusAPIService gaiusMock = (TestGaiusAPIService) orchestrator.GetService<IGaiusAPIService>();
			gaiusMock.Inhibit();
		}

		var ams = (AutoMemberSystem) orchestrator.GetService<IAutoMemberSystem>();
		await ams.Initialize();

		orchestrator.Subject.Reset();

		return orchestrator;
	}

    [Fact(DisplayName = "Eligible users should be granted membership")]
    public static async Task AutoMemberSystem_EligibleUser_ShouldBeGrantedMembership()
    {
        // Arrange
        var context = AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
            .HasPostedIntroduction()
            .WithMessages(100)
            .Build();

		var orchestrator = await SetupOrchestrator(context);
        var ams = orchestrator.GetService<IAutoMemberSystem>();

		// Act
        await ams.RunAsync();

        // Assert
        context.AssertMember(orchestrator);
    }


    [Fact(DisplayName = "Eligible users should not be granted membership if their membership is withheld.")]
    public static async Task AutoMemberSystem_EligibleUserWithAMH_ShouldNotBeGrantedMembership()
    {
        // Arrange
        var context = AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer, AutoMemberHold)
            .HasPostedIntroduction()
            .WithMessages(100)
            .Build();

		var orchestrator = await SetupOrchestrator(context);
		var ams = orchestrator.GetService<IAutoMemberSystem>();

		// Act
		await ams.RunAsync();

        // Assert
        context.AssertNotMember(orchestrator);
    }


    [Fact(DisplayName = "New users should not be granted membership.")]
    public static async Task AutoMemberSystem_NewUser_ShouldNotBeGrantedMembership()
    {
        // Arrange
        var context = AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(12))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
            .HasPostedIntroduction()
            .WithMessages(100)
            .Build();

		var orchestrator = await SetupOrchestrator(context);
		var ams = orchestrator.GetService<IAutoMemberSystem>();

		// Act
		await ams.RunAsync();

        // Assert
        context.AssertNotMember(orchestrator);
    }

    [Fact(DisplayName = "Inactive users should not be granted membership.")]
    public static async Task AutoMemberSystem_InactiveUser_ShouldNotBeGrantedMembership()
    {
        // Arrange
        var context = AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
            .HasPostedIntroduction()
            .WithMessages(10)
            .Build();

		var orchestrator = await SetupOrchestrator(context);
		var ams = orchestrator.GetService<IAutoMemberSystem>();

		// Act
		await ams.RunAsync();

        // Assert
        context.AssertNotMember(orchestrator);
    }

    [Fact(DisplayName = "Auto Member System should not affect Members.")]
    public static async Task AutoMemberSystem_Member_ShouldNotBeChanged()
    {
        // Arrange
        var context = AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(Member, Transfemme, TwentyOnePlus, SheHer)
            .HasPostedIntroduction()
            .WithMessages(100)
            .Build();

		var orchestrator = await SetupOrchestrator(context);
		var ams = orchestrator.GetService<IAutoMemberSystem>();

		// Act
		await ams.RunAsync();

        // Assert
        context.AssertUserUnchanged(orchestrator);
    }

    [Fact(DisplayName = "A user that did not post an introduction should not be granted membership")]
    public static async Task AutoMemberSystem_NoIntroduction_ShouldNotBeGrantedMembership()
    {
        // Arrange
        var context = AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
            .WithMessages(100)
            .Build();

		var orchestrator = await SetupOrchestrator(context);
		var ams = orchestrator.GetService<IAutoMemberSystem>();

		// Act
		await ams.RunAsync();
		
        // Assert
        context.AssertNotMember(orchestrator);
    }

    [Fact(DisplayName = "A user without an age role should not be granted membership")]
    public static async Task AutoMemberSystem_NoAgeRole_ShouldNotBeGrantedMembership()
    {
        // Arrange
        var context = AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, Transfemme, SheHer)
            .HasPostedIntroduction()
            .WithMessages(100)
            .Build();

		var orchestrator = await SetupOrchestrator(context);
		var ams = orchestrator.GetService<IAutoMemberSystem>();

		// Act
		await ams.RunAsync();

        // Assert
        context.AssertNotMember(orchestrator);
    }

    [Fact(DisplayName = "A user without a gender role should not be granted membership")]
    public static async Task AutoMemberSystem_NoGenderRole_ShouldNotBeGrantedMembership()
    {
        // Arrange
        var context = AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, TwentyOnePlus, SheHer)
            .HasPostedIntroduction()
            .WithMessages(100)
            .Build();

		var orchestrator = await SetupOrchestrator(context);
		var ams = orchestrator.GetService<IAutoMemberSystem>();

		// Act
		await ams.RunAsync();

        // Assert
        context.AssertNotMember(orchestrator);
    }

    [Fact(DisplayName = "A user without a pronoun role should not be granted membership")]
    public static async Task AutoMemberSystem_NoPronounRole_ShouldNotBeGrantedMembership()
    {
        // Arrange
        var context = AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus)
            .HasPostedIntroduction()
            .WithMessages(100)
            .Build();

		var orchestrator = await SetupOrchestrator(context);
		var ams = orchestrator.GetService<IAutoMemberSystem>();

		// Act
		await ams.RunAsync();

        // Assert
        context.AssertNotMember(orchestrator);
    }

    [Fact(DisplayName = "A user with a warning should not be granted membership")]
    public static async Task AutoMemberSystem_UserWithGaiusWarning_ShouldNotBeGrantedMembership()
    {
        // Arrange
        var context = AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
            .HasPostedIntroduction()
            .HasBeenWarned()
            .WithMessages(100)
            .Build();

		var orchestrator = await SetupOrchestrator(context);
		var ams = (AutoMemberSystem) orchestrator.GetService<IAutoMemberSystem>();

		TestGaiusAPIService gaiusMock = (TestGaiusAPIService) orchestrator.GetService<IGaiusAPIService>();
		gaiusMock.AddWarning(orchestrator.Subject, new Warning
		{
			Reason = "TEST PUNISHMENT",
			ModID = Snowflake.Generate(),
			UserID = orchestrator.Subject.Id
		});
		
		// reload the Gaius warnings
		await ams.Initialize();

		// Act
		await ams.RunAsync();

        // Assert
        context.AssertNotMember(orchestrator);
    }

	[Fact(DisplayName = "A user with a caselog should not be granted membership")]
	public static async Task AutoMemberSystem_UserWithGaiusCaselog_ShouldNotBeGrantedMembership()
	{
		// Arrange
		var context = AutoMemberSystemContext.Builder()
			.Joined(TimeSpan.FromHours(36))
			.SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
			.HasPostedIntroduction()
			.HasBeenPunished()
			.WithMessages(100)
			.Build();

		var orchestrator = await SetupOrchestrator(context);
		var ams = (AutoMemberSystem) orchestrator.GetService<IAutoMemberSystem>();

		TestGaiusAPIService gaiusMock = (TestGaiusAPIService) orchestrator.GetService<IGaiusAPIService>();
		gaiusMock.AddCaselog(orchestrator.Subject, new Caselog
		{
			Type = CaselogType.Mute,
			Reason = "TEST PUNISHMENT",
			ModID = Snowflake.Generate(),
			UserID = orchestrator.Subject.Id
		});

		// reload the Gaius caselogs
		await ams.Initialize();

		// Act
		await ams.RunAsync();

		// Assert
		context.AssertNotMember(orchestrator);
	}

	[Fact(DisplayName = "A user with a join age auto kick should be granted membership")]
	public static async Task AutoMemberSystem_UserWithJoinAgeKick_ShouldBeGrantedMembership()
	{
		// Arrange
		var context = AutoMemberSystemContext.Builder()
			.Joined(TimeSpan.FromHours(36))
			.SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
			.HasPostedIntroduction()
			.HasBeenPunished(true)
			.WithMessages(100)
			.Build();

		var orchestrator = await SetupOrchestrator(context);
		var ams = (AutoMemberSystem) orchestrator.GetService<IAutoMemberSystem>();
		
		TestGaiusAPIService gaiusMock = (TestGaiusAPIService) orchestrator.GetService<IGaiusAPIService>();
		gaiusMock.AddCaselog(orchestrator.Subject, new Caselog
		{
			Type = CaselogType.Kick,
			Reason = "Join age punishment",
			ModID = Snowflake.Generate(),
			UserID = orchestrator.Subject.Id
		});

		// reload the Gaius caselogs
		await ams.Initialize();

		// Act
		await ams.RunAsync();

		// Assert
		context.AssertMember(orchestrator);
	}

	[Fact(DisplayName = "A user should be granted membership if Gaius is unavailable")]
    public static async Task AutoMemberSystem_GaiusIsUnavailable_ShouldBeGrantedMembership()
    {
        // Arrange
        var context = AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
            .HasPostedIntroduction()
            .InhibitGaius()
            .WithMessages(100)
            .Build();

		var orchestrator = await SetupOrchestrator(context);
		var ams = (AutoMemberSystem) orchestrator.GetService<IAutoMemberSystem>();

		// Act
		await ams.RunAsync();

        // Assert
        context.AssertMember(orchestrator);
    }

    [Fact(DisplayName = "A user should be granted membership if they have been granted membership before")]
    public static async Task AutoMemberSystem_MemberThatRejoins_ShouldBeGrantedMembership()
    {
        // Arrange
        var context = AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(1))
            .FirstJoined(TimeSpan.FromDays(7))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
            .HasPostedIntroduction()
            .HasBeenGrantedMembershipBefore()
            .WithMessages(100)
            .Build();

		var orchestrator = await SetupOrchestrator(context);
		var ams = orchestrator.GetService<IAutoMemberSystem>();

		// Act
		var service = orchestrator.Discord as TestDiscordService;
        var user = orchestrator.Subject;

        service.Should().NotBeNull();
        user.Should().NotBeNull();

        await service.TriggerUserJoined(user);

        // Assert
        context.AssertMember(orchestrator);
    }

	[Fact(DisplayName = "The Dynamo record for a user should be updated when the user's username changes")]
	public static async Task AutoMemberSystem_MemberMetadataUpdated_ShouldBeReflectedInDynamo()
	{
		// Arrange
		const string newUsername = "fred";

		var context = AutoMemberSystemContext.Builder()
			.Joined(TimeSpan.FromHours(1))
			.FirstJoined(TimeSpan.FromDays(7))
			.SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
			.HasPostedIntroduction()
			.HasBeenGrantedMembershipBefore()
			.WithMessages(100)
			.Build();

		var orchestrator = await SetupOrchestrator(context);
		var ams = orchestrator.GetService<IAutoMemberSystem>();

		// Make sure the user is in the database
		await orchestrator.Database.CreateUserAsync(InstarUserData.CreateFrom(orchestrator.Subject));

		// Act
		var mds = (TestDiscordService) orchestrator.Discord;

		var newUser = orchestrator.Subject.Clone();
		newUser.Username = newUsername;

		await mds.TriggerUserUpdated(new UserUpdatedEventArgs(orchestrator.Subject.Id, orchestrator.Subject, newUser));

		// Assert
		var ddbUser = await orchestrator.Database.GetUserAsync(orchestrator.Subject.Id);

		ddbUser.Should().NotBeNull();
		ddbUser.Data.Username.Should().Be(newUsername);

		ddbUser.Data.Usernames.Should().NotBeNull();
		ddbUser.Data.Usernames.Count.Should().Be(2);
		ddbUser.Data.Usernames.Should().Contain(n => n.Data != null && n.Data.Equals(newUsername, StringComparison.Ordinal));
	}

	[Fact(DisplayName = "A user should be created in DynamoDB if they're eligible for membership but missing in DDB")]
	public static async Task AutoMemberSystem_MemberEligibleButMissingInDDB_ShouldBeCreatedAndGrantedMembership()
	{

		// Arrange
		var context = AutoMemberSystemContext.Builder()
			.Joined(TimeSpan.FromHours(36))
			.SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
			.HasPostedIntroduction()
			.WithMessages(100)
			.SuppressDDBEntry()
			.Build();

		var orchestrator = await SetupOrchestrator(context);
		var ams = orchestrator.GetService<IAutoMemberSystem>();

		// Act
		await ams.RunAsync();

		// Assert
		context.AssertMember(orchestrator);
	}

    private record AutoMemberSystemContext(
        int HoursSinceJoined,
        Snowflake[] Roles,
        bool PostedIntroduction,
        int MessagesLast24Hours,
        int FirstJoinTime,
        bool GrantedMembershipBefore,
		bool SuppressDDBEntry,
		bool GaiusInhibited)
    {
        public static AutoMemberSystemContextBuilder Builder() => new();

        public void AssertMember(TestOrchestrator orchestrator)
        {
			orchestrator.Subject.Should().NotBeNull();
			orchestrator.Subject.RoleIds.Should().Contain(Member.ID);
			orchestrator.Subject.RoleIds.Should().NotContain(NewMember.ID);
        }

        public void AssertNotMember(TestOrchestrator orchestrator)
        {
            orchestrator.Subject.Should().NotBeNull();
			orchestrator.Subject.RoleIds.Should().NotContain(Member.ID);
			orchestrator.Subject.RoleIds.Should().Contain(NewMember.ID);
        }

        public void AssertUserUnchanged(TestOrchestrator orchestrator)
        {
			orchestrator.Subject.Should().NotBeNull();
			orchestrator.Subject.Changed.Should().BeFalse();
        }
    }

    private class AutoMemberSystemContextBuilder
    {
        private int _hoursSinceJoined;
        private Snowflake[]? _roles;
        private bool _postedIntroduction;
        private int _messagesLast24Hours;
		private bool _gaiusAvailable = true;
		private bool _gaiusPunished;
		private bool _joinAgeKick;
		private bool _gaiusWarned;
        private int _firstJoinTime;
        private bool _grantedMembershipBefore;
		private bool _suppressDDB;


		public AutoMemberSystemContextBuilder Joined(TimeSpan timeAgo)
        {
            _hoursSinceJoined = (int) Math.Round(timeAgo.TotalHours);
            return this;
        }

        public AutoMemberSystemContextBuilder SetRoles(params Snowflake[] roles)
        {
            _roles = roles;
            return this;
        }

        public AutoMemberSystemContextBuilder HasPostedIntroduction()
        {
            _postedIntroduction = true;
            return this;
        }

        public AutoMemberSystemContextBuilder WithMessages(int messages)
        {
            _messagesLast24Hours = messages;
            return this;
        }

        public AutoMemberSystemContextBuilder InhibitGaius()
        {
            _gaiusAvailable = false;
            return this;
        }

        public AutoMemberSystemContextBuilder HasBeenPunished(bool isJoinAgeKick = false)
        {
            _gaiusPunished = true;
			_joinAgeKick = isJoinAgeKick;

			return this;
        }

        public AutoMemberSystemContextBuilder HasBeenWarned()
        {
            _gaiusWarned = true;
            return this;
        }

        public AutoMemberSystemContextBuilder FirstJoined(TimeSpan hoursAgo)
        {
            _firstJoinTime = (int) Math.Round(hoursAgo.TotalHours);
            return this;
        }

        public AutoMemberSystemContextBuilder HasBeenGrantedMembershipBefore()
        {
            _grantedMembershipBefore = true;
            return this;
		}

		public AutoMemberSystemContextBuilder SuppressDDBEntry()
		{
			_suppressDDB = true;
			return this;
		}

		public AutoMemberSystemContext Build()
        {
            return new AutoMemberSystemContext(
                _hoursSinceJoined,
                _roles ?? throw new InvalidOperationException("Roles must be set."),
                _postedIntroduction,
                _messagesLast24Hours,
                _firstJoinTime,
                _grantedMembershipBefore,
				_suppressDDB,
				_gaiusAvailable);
        }
    }
}