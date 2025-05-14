using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Moq;
using NUnit.Framework;
using UserApi.Security;
using UserApi.Services.Exceptions;

namespace UserApi.UnitTests.Services.UserAccountService
{
    public class GetGroupsForUserAsyncTests : UserAccountServiceTestsBase
    {
        private const string UserId = "userId";

        [Test]
        public async Task Should_return_groups_for_user()
        {
            // Arrange
            var groups = new List<Group>
            {
                new Group { Id = "1", DisplayName = "Group 1" },
                new Group { Id = "2", DisplayName = "Group 2" }
            };

            GraphClient.Setup(x => x.GetGroupsForUserAsync(UserId))
                .ReturnsAsync(groups);

            // Act
            var result = await Service.GetGroupsForUserAsync(UserId);

            // Assert
            result.Should().NotBeNull();
            result.Count.Should().Be(2);
            result.Should().Contain(g => g.Id == "1" && g.DisplayName == "Group 1");
            result.Should().Contain(g => g.Id == "2" && g.DisplayName == "Group 2");
        }

        [Test]
        public async Task Should_return_empty_list_when_no_groups_found()
        {
            // Arrange
            GraphClient.Setup(x => x.GetGroupsForUserAsync(UserId))
                .ReturnsAsync(new List<Group>());

            // Act
            var result = await Service.GetGroupsForUserAsync(UserId);

            // Assert
            result.Should().BeEmpty();
        }

        [Test]
        public void Should_throw_UserServiceException_on_ODataError()
        {
            // Arrange
            GraphClient.Setup(x => x.GetGroupsForUserAsync(UserId))
                .ThrowsAsync(new ODataError());

            // Act & Assert
             Assert.ThrowsAsync<UserServiceException>(async () => await Service.GetGroupsForUserAsync(UserId));
        }

        [Test]
        public void Should_throw_UserServiceException_on_unexpected_error()
        {
            // Arrange
            const string errorMessage = "Unexpected error";
            GraphClient.Setup(x => x.GetGroupsForUserAsync(UserId))
                .ThrowsAsync(new Exception(errorMessage));

            // Act & Assert
            var exception = Assert.ThrowsAsync<UserServiceException>(async () => await Service.GetGroupsForUserAsync(UserId));
            exception.Should().NotBeNull();
            exception!.Message.Should().Be($"An unexpected error occurred while retrieving groups for user {UserId}.: {errorMessage}");
        }
    }
}