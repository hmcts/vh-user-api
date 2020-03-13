using FluentAssertions;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Testing.Common.Helpers;
using UserApi.Services;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services
{
    public class CreateUserAsyncTests: UserAccountServiceTests
    {
        private const string RecoveryEmail = "test'email@com";
        private NewAdUserAccount _newAdUserAccount;

        [SetUp]
        public void Init()
        {
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(AzureAdGraphQueryResponse, HttpStatusCode.OK));

            _newAdUserAccount = new NewAdUserAccount { Username = "TestUser", UserId = "TestUserId", OneTimePassword = "OTPwd" };
            IdentityServiceApiClient.Setup(i => i.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(_newAdUserAccount); 
        }

        [Test]
        public async Task Should_create_new_user_account_successfully()
        {
            var existingUsers = new[] { "existing.user", "existing.user1" };
            IdentityServiceApiClient
                .Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUsers.Select(username => username + Domain));

            Filter = $"otherMails/any(c:c eq '{RecoveryEmail.Replace("'", "''")}')";

            AzureAdGraphQueryResponse.Value = new List<AzureAdGraphUserResponse>();
            SecureHttpRequest
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(AzureAdGraphQueryResponse, HttpStatusCode.OK));

            var response = await Service.CreateUserAsync("fName", "lName", RecoveryEmail);

            response.Should().NotBeNull();
            response.Username.Should().Be(_newAdUserAccount.Username);
            response.UserId.Should().Be(_newAdUserAccount.UserId);
            response.OneTimePassword.Should().Be(_newAdUserAccount.OneTimePassword);
            SecureHttpRequest.Verify(s => s.GetAsync(It.IsAny<string>(), AccessUri), Times.Once);
            IdentityServiceApiClient.Verify(i => i.CreateUserAsync(It.IsAny<string>(), "fName", "lName", "fName lName", RecoveryEmail), Times.Once);
        }

        [Test]
        public void Should_return_user_already_exists_with_recovery_email()
        {
            Filter = $"otherMails/any(c:c eq '{RecoveryEmail.Replace("'", "''")}')"; 

            var response = Assert.ThrowsAsync<UserExistsException>(async () => await Service.CreateUserAsync("fName", "lName", RecoveryEmail));

            response.Message.Should().Be("User with recovery email already exists");
        }
    }
}
