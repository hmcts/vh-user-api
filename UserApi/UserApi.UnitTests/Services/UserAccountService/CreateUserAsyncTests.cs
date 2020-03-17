using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Testing.Common.Helpers;
using UserApi.Services;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services.UserAccountService
{
    public class CreateUserAsyncTests: UserAccountServiceTests
    {
        private const string RecoveryEmail = "test'email@com";
        private NewAdUserAccount _newAdUserAccount;

        [SetUp]
        public new void Setup()
        {
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(azureAdGraphQueryResponse, HttpStatusCode.OK));

            _newAdUserAccount = new NewAdUserAccount { Username = "TestUser", UserId = "TestUserId", OneTimePassword = "OTPwd" };
            _identityServiceApiClient.Setup(i => i.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(_newAdUserAccount); 
        }

        [Test]
        public async Task Should_create_new_user_account_successfully()
        {
            var existingUsers = new[] { "existing.user", "existing.user1" };
            _identityServiceApiClient.Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUsers.Select(username => username + Domain));

            filter = $"otherMails/any(c:c eq '{RecoveryEmail.Replace("'", "''")}')";

            azureAdGraphQueryResponse.Value = new List<AzureAdGraphUserResponse>();
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(azureAdGraphQueryResponse, HttpStatusCode.OK));


            var response = await _service.CreateUserAsync("fName", "lName", RecoveryEmail);

            response.Should().NotBeNull();
            response.Username.Should().Be(_newAdUserAccount.Username);
            response.UserId.Should().Be(_newAdUserAccount.UserId);
            response.OneTimePassword.Should().Be(_newAdUserAccount.OneTimePassword);
            _secureHttpRequest.Verify(s => s.GetAsync(It.IsAny<string>(), AccessUri), Times.Once);
            _identityServiceApiClient.Verify(i => i.CreateUserAsync(It.IsAny<string>(), "fName", "lName", "fName lName", RecoveryEmail), Times.Once);
        }

        [Test]
        public async Task Should_return_user_already_exists_with_recovery_email()
        {
            filter = $"otherMails/any(c:c eq '{RecoveryEmail.Replace("'", "''")}')"; 


            var response = Assert.ThrowsAsync<UserExistsException>(async () => await _service.CreateUserAsync("fName", "lName", RecoveryEmail));


            response.Message.Should().Be("User with recovery email already exists");
        }
    }
}
