using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Moq;
using NUnit.Framework;
using UserApi.Security;
using UserApi.Services.Exceptions;

namespace UserApi.UnitTests.Services.UserAccountService;

public class CreateUserAsyncTests : UserAccountServiceTestsBase
{
    private const string FirstName = "Test";
    private const string LastName = "User";
    private const string RecoveryEmail = "testuser@example.com";

    [Test]
    public async Task Should_create_user_successfully()
    {
        // Arrange
        var newUser = new User
        {
            DisplayName = $"{FirstName} {LastName}",
            UserPrincipalName = "test.user@example.com",
            Id = "12345"
        };

        GraphClient.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync(newUser);

        // Act
        var result = await Service.CreateUserAsync(FirstName, LastName, RecoveryEmail, false);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be(newUser.UserPrincipalName);
        result.UserId.Should().Be(newUser.Id);
        GraphClient.Verify(x => x.CreateUserAsync(It.IsAny<User>()), Times.Once);
    }

    [Test]
    public void Should_throw_InvalidEmailException_for_invalid_recovery_email()
    {
        // Arrange
        const string invalidEmail = "invalid-email";

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidEmailException>(async () =>
            await Service.CreateUserAsync(FirstName, LastName, invalidEmail, false));
        exception.Should().NotBeNull();
        exception!.Message.Should().Be("Recovery email is not a valid email");
    }

    [Test]
    public void Should_throw_UserExistsException_when_user_with_recovery_email_exists()
    {
        // Arrange
        var existingUser = new User { UserPrincipalName = "existing.user@example.com" };
        GraphClient.Setup(x => x.GetUsersAsync(It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync(new List<User> { existingUser });

        // Act & Assert
        var exception = Assert.ThrowsAsync<UserExistsException>(async () =>
            await Service.CreateUserAsync(FirstName, LastName, RecoveryEmail, false));
        exception.Should().NotBeNull();
        exception!.Message.Should().Be("User with recovery email already exists");
    }

    [Test]
    public void Should_throw_UserServiceException_on_ODataError()
    {
        // Arrange
        GraphClient.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
            .ThrowsAsync(new ODataError());

        // Act & Assert
       Assert.ThrowsAsync<UserServiceException>(async () => await Service.CreateUserAsync(FirstName, LastName, RecoveryEmail, false));
    }

    [Test]
    public void Should_throw_UserServiceException_on_unexpected_error()
    {
        // Arrange
        const string errorMessage = "Unexpected error";
        GraphClient.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
            .ThrowsAsync(new Exception(errorMessage));

        // Act & Assert
        var exception = Assert.ThrowsAsync<UserServiceException>(async () =>
            await Service.CreateUserAsync(FirstName, LastName, RecoveryEmail, false));
        exception.Should().NotBeNull();
        exception!.Message.Should().Be($"An unexpected error occurred while creating the user.: {errorMessage}");
    }
}