using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PaxAndromeda.Instar;
using PaxAndromeda.Instar.Preconditions;
using Xunit;

namespace InstarBot.Tests.Preconditions;

public sealed class RequireStaffMemberAttributeTests
{
    [Fact]
    public async Task CheckRequirementsAsync_ShouldReturnFalse_WithBadConfig()
    {
        // Arrange
        var attr = new RequireStaffMemberAttribute();
        var serviceColl = new ServiceCollection();
        serviceColl.AddSingleton(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build());

        var context = TestUtilities.SetupContext(new TestContext
        {
            UserRoles = [new Snowflake(793607635608928257)]
        });

        // Act
        var result = await attr.CheckRequirementsAsync(context.Object, null!, serviceColl.BuildServiceProvider());

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CheckRequirementsAsync_ShouldReturnFalse_WithNonGuildUser()
    {
        // Arrange
        var attr = new RequireStaffMemberAttribute();

        var context = new Mock<IInteractionContext>();
        context.Setup(n => n.User).Returns(Mock.Of<IUser>());

        // Act
        var result = await attr.CheckRequirementsAsync(context.Object, null!, TestUtilities.GetServices());

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CheckRequirementsAsync_ShouldReturnSuccessful_WithValidUser()
    {
        // Arrange
        var attr = new RequireStaffMemberAttribute();

        var context = TestUtilities.SetupContext(new TestContext
        {
            UserRoles = [new Snowflake(793607635608928257)]
        });

        // Act
        var result = await attr.CheckRequirementsAsync(context.Object, null!, TestUtilities.GetServices());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CheckRequirementsAsync_ShouldReturnFailure_WithNonStaffUser()
    {
        // Arrange
        var attr = new RequireStaffMemberAttribute();

        var context = TestUtilities.SetupContext(new TestContext
        {
            UserRoles = [new Snowflake()]
        });

        // Act
        var result = await attr.CheckRequirementsAsync(context.Object, null!, TestUtilities.GetServices());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorReason.Should().Be("You are not eligible to run this command");
    }
}