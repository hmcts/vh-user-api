using System;
using System.Net;
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

public class UpdateUserAccountAsyncTests : UserAccountServiceTestsBase
{
    private Guid _userId;
    private User _existingUser;
    private const string FirstName = "FirstName";
    private const string LastName = "LastName";
    private const string ContactEmail = "email@email.com";
    private const string Username = "username@email.com";
    
    [SetUp]
    public new void Setup()
    {
        
        _userId = Guid.NewGuid();
        _existingUser = new User
        {
            Id = _userId.ToString(),
            GivenName = "OldFirstName",
            Surname = "OldLastName",
            UserPrincipalName = Username
        };
    }
    
    [Test]
    public async Task Should_update_user_account()
    {
        // Arrange
        var updatedUser = new User
        {
            GivenName = FirstName,
            Surname = LastName,
            DisplayName = $"{FirstName} {LastName}",
            UserPrincipalName = Username,
            Mail = ContactEmail,
            OtherMails = [ContactEmail]
        };

        GraphClient
            .SetupSequence(client => client.GetUserAsync(It.IsAny<string>()))
            .ReturnsAsync(_existingUser)
            .ReturnsAsync(updatedUser);

        GraphClient
            .Setup(client => client.UpdateUserAsync(_userId.ToString(), updatedUser))
            .Verifiable();

        // Act
        var result = await Service.UpdateUserAccountAsync(_userId, FirstName, LastName, ContactEmail);

        // Assert
        GraphClient.Verify(client => client.UpdateUserAsync(_userId.ToString(), It.IsAny<User>()), Times.Once);
        GraphClient.Verify(client => client.GetUserAsync(_userId.ToString()), Times.Exactly(2));
        
        result.GivenName.Should().Be(updatedUser.GivenName);
        result.Surname.Should().Be(updatedUser.Surname);
        result.UserPrincipalName.Should().Be(updatedUser.UserPrincipalName);
        result.Mail.Should().Be(updatedUser.Mail);
        result.OtherMails.Should().BeEquivalentTo(updatedUser.OtherMails);
    }

    [Test]
    public void Should_throw_exception_when_user_does_not_exist()
    {
        // Arrange
        GraphClient
            .Setup(client => client.GetUsersAsync(It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync([_existingUser]);
        
        GraphClient
            .Setup(client => client.UpdateUserAsync(_userId.ToString(), It.IsAny<User>())).ThrowsAsync(new Exception());


        // Act & Assert
        Assert.ThrowsAsync<UserServiceException>(async () => await Service.UpdateUserAccountAsync(_userId, FirstName, LastName, ContactEmail));
    }

    [Test]
    public void Should_throw_UserDoesNotExistException_on_ODataError404()
    {
        var error = new ODataError { ResponseStatusCode = (int)HttpStatusCode.NotFound };
        // Arrange
        GraphClient
            .Setup(client => client.GetUserAsync(It.IsAny<string>()))
            .ThrowsAsync(error);

        // Act & Assert
        Assert.ThrowsAsync<UserDoesNotExistException>(async () => await Service.UpdateUserAccountAsync(_userId, FirstName, LastName, ContactEmail));
    }
}