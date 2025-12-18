using Discord;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PaxAndromeda.Instar.Preconditions;
using System.Collections.Generic;
using System.Threading.Tasks;
using InstarBot.Test.Framework;
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

		var orchestrator = TestOrchestrator.Default;
		await orchestrator.Subject.AddRoleAsync(orchestrator.Configuration.StaffRoleID);

		var context = new Mock<IInteractionContext>();
		context.Setup(n => n.User).Returns(orchestrator.Subject);

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

		var orchestrator = TestOrchestrator.Default;

		var context = new Mock<IInteractionContext>();
        context.Setup(n => n.User).Returns(Mock.Of<IUser>());

        // Act
        var result = await attr.CheckRequirementsAsync(context.Object, null!, orchestrator.ServiceProvider);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CheckRequirementsAsync_ShouldReturnSuccessful_WithValidUser()
    {
        // Arrange
        var attr = new RequireStaffMemberAttribute();

		var orchestrator = TestOrchestrator.Default;
		await orchestrator.Subject.AddRoleAsync(orchestrator.Configuration.StaffRoleID);

		var context = new Mock<IInteractionContext>();
		context.Setup(n => n.User).Returns(orchestrator.Subject);

		// Act
		var result = await attr.CheckRequirementsAsync(context.Object, null!, orchestrator.ServiceProvider);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CheckRequirementsAsync_ShouldReturnFailure_WithNonStaffUser()
    {
        // Arrange
        var attr = new RequireStaffMemberAttribute();
        
		var orchestrator = TestOrchestrator.Default;
		await orchestrator.Subject.AddRoleAsync(orchestrator.Configuration.NewMemberRoleID);

		var context = new Mock<IInteractionContext>();
		context.Setup(n => n.User).Returns(orchestrator.Subject);

		// Act
		var result = await attr.CheckRequirementsAsync(context.Object, null!, orchestrator.ServiceProvider);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorReason.Should().Be("You are not eligible to run this command");
    }
}