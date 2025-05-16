using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Graph.Models;
    using Moq;
    using NUnit.Framework;
    
    namespace UserApi.UnitTests.Services.UserAccountService;
    
    public class CheckForNextAvailableNameTests : UserAccountServiceTestsBase
    {
        [Test]
        public async Task Should_increment_the_username_when_existing_users_found()
        {
            // Arrange
            const string firstName = "Existing";
            const string lastName = "User";
            var existingUsers = new List<string> { "existing.user", "existing.user1" };
    
            GraphClient.Setup(x => x.GetUsersAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(existingUsers.Select(username => new User { UserPrincipalName = username + "@example.com" }).ToList());
    
            // Act
            var nextAvailable = await Service.CheckForNextAvailableUsernameAsync(firstName, lastName, null);
    
            // Assert
            nextAvailable.Should().Be("existing.user2@example.com");
            GraphClient.Verify(x => x.GetUsersAsync(It.IsAny<string>(), CancellationToken.None), Times.Once);
        }
    
        [Test]
        public async Task Should_return_base_username_when_no_existing_users_found()
        {
            // Arrange
            const string firstName = "New";
            const string lastName = "User";
            var baseUsername = $"{firstName}.{lastName}".ToLowerInvariant();
    
            GraphClient.Setup(x => x.GetUsersAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(new List<User>());
    
            // Act
            var nextAvailable = await Service.CheckForNextAvailableUsernameAsync(firstName, lastName, null);
    
            // Assert
            nextAvailable.Should().Be($"{baseUsername}@example.com");
            GraphClient.Verify(x => x.GetUsersAsync(It.IsAny<string>(), CancellationToken.None), Times.Once);
        }
    
        [Test]
        public async Task Should_sanitise_names_before_generating_username()
        {
            // Arrange
            const string firstName = ".First.";
            const string lastName = ".La.st.";
            const string contactEmail = "first.name@test.com";
            var baseUsername = "first.la.st";
    
            GraphClient.Setup(x => x.GetUsersAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(new List<User>());
    
            // Act
            var nextAvailable = await Service.CheckForNextAvailableUsernameAsync(firstName, lastName, contactEmail);
    
            // Assert
            nextAvailable.Should().Be($"{baseUsername}@example.com");
            GraphClient.Verify(x => x.GetUsersAsync(It.IsAny<string>(), CancellationToken.None), Times.Once);
        }
    
        [Test]
        public async Task Should_handle_deleted_users_with_matching_email()
        {
            // Arrange
            const string firstName = "Deleted";
            const string lastName = "User";
            const string contactEmail = "deleted.user@example.com";
            var deletedUsers = new List<string> { contactEmail };
    
            GraphClient.Setup(x => x.GetUsersAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(new List<User>());
            GraphClient.Setup(x => x.GetDeletedUsernamesAsync(It.IsAny<string>()))
                .ReturnsAsync(deletedUsers);
    
            // Act
            var nextAvailable = await Service.CheckForNextAvailableUsernameAsync(firstName, lastName, contactEmail);
    
            // Assert
            nextAvailable.Should().Be("deleted.user1@example.com");
            GraphClient.Verify(x => x.GetUsersAsync(It.IsAny<string>(), CancellationToken.None), Times.Once);
            GraphClient.Verify(x => x.GetDeletedUsernamesAsync(It.IsAny<string>()), Times.Exactly(2));
        }
    }