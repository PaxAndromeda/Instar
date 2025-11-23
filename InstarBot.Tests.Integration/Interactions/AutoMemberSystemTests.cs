using FluentAssertions;
using InstarBot.Tests.Models;
using InstarBot.Tests.Services;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.ConfigModels;
using PaxAndromeda.Instar.DynamoModels;
using PaxAndromeda.Instar.Gaius;
using PaxAndromeda.Instar.Modals;
using PaxAndromeda.Instar.Services;
using Xunit;

namespace InstarBot.Tests.Integration.Interactions;

public static class AutoMemberSystemTests
{
    private static readonly Snowflake NewMember = new(796052052433698817);
    private static readonly Snowflake Member = new(793611808372031499);
    private static readonly Snowflake Transfemme = new(796085775199502357);
    private static readonly Snowflake TwentyOnePlus = new(796148869855576064);
    private static readonly Snowflake SheHer = new(796578609535647765);
    private static readonly Snowflake AutoMemberHold = new(966434762032054282);

    private static AutoMemberSystem SetupTest(AutoMemberSystemContext scenarioContext)
    {
        var testContext = scenarioContext.TestContext;

        var discordService = TestUtilities.SetupDiscordService(testContext);
        var gaiusApiService = TestUtilities.SetupGaiusAPIService(testContext);
        var config = TestUtilities.GetDynamicConfiguration();

        scenarioContext.DiscordService = discordService;
        var userId = scenarioContext.UserID;
        var relativeJoinTime = scenarioContext.HoursSinceJoined;
        var roles = scenarioContext.Roles;
        var postedIntro = scenarioContext.PostedIntroduction;
        var messagesLast24Hours = scenarioContext.MessagesLast24Hours;
        var firstSeenTime = scenarioContext.FirstJoinTime;
        var grantedMembershipBefore = scenarioContext.GrantedMembershipBefore;

        var amsConfig = scenarioContext.Config.AutoMemberConfig;

        var ddbService = new MockInstarDDBService();

		var user = new TestGuildUser
		{
			Id = userId,
			Username = "username",
			JoinedAt = DateTimeOffset.Now - TimeSpan.FromHours(relativeJoinTime),
			RoleIds = roles.Select(n => n.ID).ToList().AsReadOnly()
		};

		var userData = InstarUserData.CreateFrom(user);
		userData.Position = grantedMembershipBefore ? InstarUserPosition.Member : InstarUserPosition.NewMember;

		if (!scenarioContext.SuppressDDBEntry)
			ddbService.Register(userData);

        testContext.AddRoles(roles);

        testContext.GuildUsers.Add(user);


        var genericChannel = Snowflake.Generate();
        testContext.AddChannel(amsConfig.IntroductionChannel);

        testContext.AddChannel(genericChannel);
        if (postedIntro)
            testContext.GetChannel(amsConfig.IntroductionChannel).AddMessage(user, "Some text");

        for (var i = 0; i < messagesLast24Hours; i++)
            testContext.GetChannel(genericChannel).AddMessage(user, "Some text");


        var ams = new AutoMemberSystem(config, discordService, gaiusApiService, ddbService, new MockMetricService());

        scenarioContext.User = user;
		scenarioContext.DynamoService = ddbService;

        return ams;
    }


    [Fact(DisplayName = "Eligible users should be granted membership")]
    public static async Task AutoMemberSystem_EligibleUser_ShouldBeGrantedMembership()
    {
        // Arrange
        var context = await AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
            .HasPostedIntroduction()
            .WithMessages(100)
            .Build();

        var ams = SetupTest(context);

        // Act
        await ams.RunAsync();

        // Assert
        context.AssertMember();
    }


    [Fact(DisplayName = "Eligible users should not be granted membership if their membership is withheld.")]
    public static async Task AutoMemberSystem_EligibleUserWithAMH_ShouldNotBeGrantedMembership()
    {
        // Arrange
        var context = await AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer, AutoMemberHold)
            .HasPostedIntroduction()
            .WithMessages(100)
            .Build();

        var ams = SetupTest(context);

        // Act
        await ams.RunAsync();

        // Assert
        context.AssertNotMember();
    }


    [Fact(DisplayName = "New users should not be granted membership.")]
    public static async Task AutoMemberSystem_NewUser_ShouldNotBeGrantedMembership()
    {
        // Arrange
        var context = await AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(12))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
            .HasPostedIntroduction()
            .WithMessages(100)
            .Build();

        var ams = SetupTest(context);

        // Act
        await ams.RunAsync();

        // Assert
        context.AssertNotMember();
    }

    [Fact(DisplayName = "Inactive users should not be granted membership.")]
    public static async Task AutoMemberSystem_InactiveUser_ShouldNotBeGrantedMembership()
    {
        // Arrange
        var context = await AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
            .HasPostedIntroduction()
            .WithMessages(10)
            .Build();

        var ams = SetupTest(context);

        // Act
        await ams.RunAsync();

        // Assert
        context.AssertNotMember();
    }

    [Fact(DisplayName = "Auto Member System should not affect Members.")]
    public static async Task AutoMemberSystem_Member_ShouldNotBeChanged()
    {
        // Arrange
        var context = await AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(Member, Transfemme, TwentyOnePlus, SheHer)
            .HasPostedIntroduction()
            .WithMessages(100)
            .Build();

        var ams = SetupTest(context);

        // Act
        await ams.RunAsync();

        // Assert
        context.AssertUserUnchanged();
    }

    [Fact(DisplayName = "A user that did not post an introduction should not be granted membership")]
    public static async Task AutoMemberSystem_NoIntroduction_ShouldNotBeGrantedMembership()
    {
        // Arrange
        var context = await AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
            .WithMessages(100)
            .Build();

        var ams = SetupTest(context);

        // Act
        await ams.RunAsync();

        // Assert
        context.AssertNotMember();
    }

    [Fact(DisplayName = "A user without an age role should not be granted membership")]
    public static async Task AutoMemberSystem_NoAgeRole_ShouldNotBeGrantedMembership()
    {
        // Arrange
        var context = await AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, Transfemme, SheHer)
            .HasPostedIntroduction()
            .WithMessages(100)
            .Build();

        var ams = SetupTest(context);

        // Act
        await ams.RunAsync();

        // Assert
        context.AssertNotMember();
    }

    [Fact(DisplayName = "A user without a gender role should not be granted membership")]
    public static async Task AutoMemberSystem_NoGenderRole_ShouldNotBeGrantedMembership()
    {
        // Arrange
        var context = await AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, TwentyOnePlus, SheHer)
            .HasPostedIntroduction()
            .WithMessages(100)
            .Build();

        var ams = SetupTest(context);

        // Act
        await ams.RunAsync();

        // Assert
        context.AssertNotMember();
    }

    [Fact(DisplayName = "A user without a pronoun role should not be granted membership")]
    public static async Task AutoMemberSystem_NoPronounRole_ShouldNotBeGrantedMembership()
    {
        // Arrange
        var context = await AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus)
            .HasPostedIntroduction()
            .WithMessages(100)
            .Build();

        var ams = SetupTest(context);

        // Act
        await ams.RunAsync();

        // Assert
        context.AssertNotMember();
    }

    [Fact(DisplayName = "A user with a warning should not be granted membership")]
    public static async Task AutoMemberSystem_UserWithGaiusWarning_ShouldNotBeGrantedMembership()
    {
        // Arrange
        var context = await AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
            .HasPostedIntroduction()
            .HasBeenWarned()
            .WithMessages(100)
            .Build();

        var ams = SetupTest(context);

        // Act
        await ams.RunAsync();

        // Assert
        context.AssertNotMember();
    }

    [Fact(DisplayName = "A user with a caselog should not be granted membership")]
    public static async Task AutoMemberSystem_UserWithGaiusCaselog_ShouldNotBeGrantedMembership()
    {
        // Arrange
        var context = await AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
            .HasPostedIntroduction()
            .HasBeenPunished()
            .WithMessages(100)
            .Build();

        var ams = SetupTest(context);

        // Act
        await ams.RunAsync();

        // Assert
        context.AssertNotMember();
    }

    [Fact(DisplayName = "A user should be granted membership if Gaius is unavailable")]
    public static async Task AutoMemberSystem_GaiusIsUnavailable_ShouldBeGrantedMembership()
    {
        // Arrange
        var context = await AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(36))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
            .HasPostedIntroduction()
            .InhibitGaius()
            .WithMessages(100)
            .Build();

        var ams = SetupTest(context);

        // Act
        await ams.RunAsync();

        // Assert
        context.AssertMember();
    }

    [Fact(DisplayName = "A user should be granted membership if they have been granted membership before")]
    public static async Task AutoMemberSystem_MemberThatRejoins_ShouldBeGrantedMembership()
    {
        // Arrange
        var context = await AutoMemberSystemContext.Builder()
            .Joined(TimeSpan.FromHours(1))
            .FirstJoined(TimeSpan.FromDays(7))
            .SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
            .HasPostedIntroduction()
            .HasBeenGrantedMembershipBefore()
            .WithMessages(100)
            .Build();

        SetupTest(context);

        // Act
        var service = context.DiscordService as MockDiscordService;
        var user = context.User;

        service.Should().NotBeNull();
        user.Should().NotBeNull();

        await service.TriggerUserJoined(user);

        // Assert
        context.AssertMember();
    }

	[Fact(DisplayName = "The Dynamo record for a user should be updated when the user's username changes")]
	public static async Task AutoMemberSystem_MemberMetadataUpdated_ShouldBeReflectedInDynamo()
	{
		// Arrange
		const string NewUsername = "fred";

		var context = await AutoMemberSystemContext.Builder()
			.Joined(TimeSpan.FromHours(1))
			.FirstJoined(TimeSpan.FromDays(7))
			.SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
			.HasPostedIntroduction()
			.HasBeenGrantedMembershipBefore()
			.WithMessages(100)
			.Build();

		SetupTest(context);

		// Make sure the user is in the database
		context.DynamoService.Should().NotBeNull(because: "Test is invalid if DynamoService is not set");
		await context.DynamoService.CreateUserAsync(InstarUserData.CreateFrom(context.User!));

		// Act
		MockDiscordService mds = (MockDiscordService) context.DiscordService!;

		var newUser = context.User!.Clone();
		newUser.Username = NewUsername;

		await mds.TriggerUserUpdated(new UserUpdatedEventArgs(context.UserID, context.User, newUser));

		// Assert
		var ddbUser = await context.DynamoService.GetUserAsync(context.UserID);

		ddbUser.Should().NotBeNull();
		ddbUser.Data.Username.Should().Be(NewUsername);

		ddbUser.Data.Usernames.Should().NotBeNull();
		ddbUser.Data.Usernames.Count.Should().Be(2);
		ddbUser.Data.Usernames.Should().Contain(n => n.Data != null && n.Data.Equals(NewUsername, StringComparison.Ordinal));
	}

	[Fact(DisplayName = "A user should be created in DynamoDB if they're eligible for membership but missing in DDB")]
	public static async Task AutoMemberSystem_MemberEligibleButMissingInDDB_ShouldBeCreatedAndGrantedMembership()
	{

		// Arrange
		var context = await AutoMemberSystemContext.Builder()
			.Joined(TimeSpan.FromHours(36))
			.SetRoles(NewMember, Transfemme, TwentyOnePlus, SheHer)
			.HasPostedIntroduction()
			.WithMessages(100)
			.SuppressDDBEntry()
			.Build();

		var ams = SetupTest(context);

		// Act
		await ams.RunAsync();

		// Assert
		context.AssertMember();
	}

    private record AutoMemberSystemContext(
        Snowflake UserID,
        int HoursSinceJoined,
        Snowflake[] Roles,
        bool PostedIntroduction,
        int MessagesLast24Hours,
        int FirstJoinTime,
        bool GrantedMembershipBefore,
		bool SuppressDDBEntry,
        TestContext TestContext,
        InstarDynamicConfiguration Config)
    {
        public static AutoMemberSystemContextBuilder Builder() => new();

        public IDiscordService? DiscordService { get; set; }
        public TestGuildUser? User { get; set; }
        public IInstarDDBService? DynamoService { get ; set ; }

        public void AssertMember()
        {
            User.Should().NotBeNull();
            User.RoleIds.Should().Contain(Member.ID);
            User.RoleIds.Should().NotContain(NewMember.ID);
        }

        public void AssertNotMember()
        {
            User.Should().NotBeNull();
            User.RoleIds.Should().NotContain(Member.ID);
            User.RoleIds.Should().Contain(NewMember.ID);
        }

        public void AssertUserUnchanged()
        {
            User.Should().NotBeNull();
            User.Changed.Should().BeFalse();
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

        public AutoMemberSystemContextBuilder HasBeenPunished()
        {
            _gaiusPunished = true;
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

		public async Task<AutoMemberSystemContext> Build()
        {
            var config = await TestUtilities.GetDynamicConfiguration().GetConfig();

            var testContext = new TestContext();

            var userId = Snowflake.Generate();

            // Set up any warnings or whatnot
            testContext.InhibitGaius = !_gaiusAvailable;

            if (_gaiusPunished)
            {
                testContext.AddCaselog(userId, new Caselog
                {
                    Type = CaselogType.Mute,
                    Reason = "TEST PUNISHMENT",
                    ModID = Snowflake.Generate(),
                    UserID = userId
                });
            }
            if (_gaiusWarned)
            {
                testContext.AddWarning(userId, new Warning
                {
                    Reason = "TEST PUNISHMENT",
                    ModID = Snowflake.Generate(),
                    UserID = userId
                });
            }

            return new AutoMemberSystemContext(
                userId,
                _hoursSinceJoined,
                _roles ?? throw new InvalidOperationException("Roles must be set."),
                _postedIntroduction,
                _messagesLast24Hours,
                _firstJoinTime,
                _grantedMembershipBefore,
				_suppressDDB,
				testContext,
                config);
        }
    }
}