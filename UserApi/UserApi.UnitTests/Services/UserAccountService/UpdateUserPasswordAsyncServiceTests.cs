using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using UserApi.Security;

namespace UserApi.UnitTests.Services.UserAccountService;

[TestFixture]
public class UpdateUserPasswordAsyncServiceTests : UserAccountServiceTestsBase
{
    [Test]
    public async Task Should_be_successful_response_with_new_password_on_update()
    {
        // Arrange
        var username = "test.user@domain.com";
        GraphClient.Setup(client => client.UpdateUserAsync(username, It.IsAny<User>()))
            .Verifiable();

        // Act
        var result = await Service.UpdateUserPasswordAsync(username);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().Be(12);
    }
    

    [Test]
    public void Should_throw_UserServiceException_on_ODataError()
    {
        // Arrange
        var username = "test.user@domain.com";
        GraphClient.Setup(client => client.UpdateUserAsync(username, It.IsAny<User>()))
            .ThrowsAsync(new ODataError());
            
        // Act & Assert
        Assert.ThrowsAsync<UserServiceException>(() => Service.UpdateUserPasswordAsync(username));
    }

    [Test]
    public void Should_throw_UserServiceException_on_generic_exception()
    {
        // Arrange
        var username = "test.user@domain.com";
        GraphClient.Setup(client => client.UpdateUserAsync(username, It.IsAny<User>()))
            .ThrowsAsync(new Exception("Generic error"));

        // Act & Assert
       Assert.ThrowsAsync<UserServiceException>(async ()=> await Service.UpdateUserPasswordAsync(username));
    }
}