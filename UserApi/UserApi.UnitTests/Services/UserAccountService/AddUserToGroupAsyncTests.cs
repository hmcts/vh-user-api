using System.Collections.Generic;
using System.Net;
        using System.Threading.Tasks;
        using FluentAssertions;
        using Microsoft.Graph.Models;
        using Moq;
        using NUnit.Framework;
        using UserApi.Security;
        
        namespace UserApi.UnitTests.Services.UserAccountService;
        
        public class AddUserToGroupAsyncTests : UserAccountServiceTestsBase
        {
            private const string UserId = "testUserId";
            private const string GroupId = "testGroupId";
        
            [Test]
            public async Task Should_add_user_to_group_successfully()
            {
                // Arrange
                GraphClient.Setup(x => x.GetGroupsForUserAsync(UserId))
                    .ReturnsAsync(new List<Group>());
        
                GraphClient.Setup(x => x.AddUserToGroupAsync(UserId, GroupId))
                    .Returns(Task.CompletedTask);
        
                // Act
                await Service.AddUserToGroupAsync(UserId, GroupId);
        
                // Assert
                GraphClient.Verify(x => x.GetGroupsForUserAsync(UserId), Times.Once);
                GraphClient.Verify(x => x.AddUserToGroupAsync(UserId, GroupId), Times.Once);
            }
        
            [Test]
            public async Task Should_not_add_user_to_group_if_already_in_group()
            {
                // Arrange
                var existingGroups = new List<Group> { new Group { Id = GroupId } };
                GraphClient.Setup(x => x.GetGroupsForUserAsync(UserId))
                    .ReturnsAsync(existingGroups);
        
                // Act
                await Service.AddUserToGroupAsync(UserId, GroupId);
        
                // Assert
                GraphClient.Verify(x => x.GetGroupsForUserAsync(UserId), Times.Once);
                GraphClient.Verify(x => x.AddUserToGroupAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            }
        
            [Test]
            public void Should_throw_UserServiceException_on_error()
            {
                // Arrange
                const string errorMessage = "Unexpected error";
                GraphClient.Setup(x => x.GetGroupsForUserAsync(UserId))
                    .ReturnsAsync(new List<Group>());
        
                GraphClient.Setup(x => x.AddUserToGroupAsync(UserId, GroupId))
                    .ThrowsAsync(new UserServiceException($"Failed to add user {UserId} to group {GroupId}.", errorMessage));
        
                // Act & Assert
                Assert.ThrowsAsync<UserServiceException>(async () => await Service.AddUserToGroupAsync(UserId, GroupId));
            }
        }