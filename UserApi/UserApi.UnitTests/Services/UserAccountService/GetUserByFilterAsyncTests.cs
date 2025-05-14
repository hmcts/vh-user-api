using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Moq;
using NUnit.Framework;
using UserApi.Security;

namespace UserApi.UnitTests.Services.UserAccountService;

[TestFixture]
public class GetUserByFilterAsyncTests : UserAccountServiceTestsBase
{
    private string _filter = "testFilter";


    [Test]
    public async Task Should_return_user_by_given_filter()
    {
        // Arrange
        GraphClient.Setup(client => client.GetUsersAsync(_filter, CancellationToken.None))
            .ReturnsAsync(new List<User>{ new() { Id = "1", DisplayName = "Test User" } })
            .Verifiable();

        // Act
        var response = await Service.GetUserByFilterAsync(_filter);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("1");
        response.DisplayName.Should().Be("Test User");
    }

    [Test]
    public void Should_return_user_exception_for_other_responses()
    {
        // Arrange
        GraphClient.Setup(client => client.GetUsersAsync(_filter, CancellationToken.None))
            .ThrowsAsync(new ODataError());
        // Act
       Assert.ThrowsAsync<UserServiceException>(async () => await Service.GetUserByFilterAsync(_filter));
    }
}