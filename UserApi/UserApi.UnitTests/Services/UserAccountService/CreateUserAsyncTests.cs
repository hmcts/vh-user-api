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
    public class CreateUserAsyncTests : UserAccountServiceTests
    {
        private const string RecoveryEmail = "test'email@a.com";
        private NewAdUserAccount _newAdUserAccount;

        [SetUp]
        public new void Setup()
        {
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(AzureAdGraphQueryResponse, HttpStatusCode.OK));

            _newAdUserAccount = new NewAdUserAccount { Username = "TestUser", UserId = "TestUserId", OneTimePassword = "OTPwd" };
            IdentityServiceApiClient.Setup(i => i.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(_newAdUserAccount);
        }

        [Test]
        public async Task Should_create_new_user_account_successfully()
        {
            var existingUsers = new[] { "existing.user", "existing.user1" };
            IdentityServiceApiClient.Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUsers.Select(username => username + Domain));

            Filter = $"otherMails/any(c:c eq '{RecoveryEmail.Replace("'", "''")}')";

            AzureAdGraphQueryResponse.Value = new List<AzureAdGraphUserResponse>();
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(AzureAdGraphQueryResponse, HttpStatusCode.OK));


            var response = await Service.CreateUserAsync("fName", "lName", RecoveryEmail, false);

            response.Should().NotBeNull();
            response.Username.Should().Be(_newAdUserAccount.Username);
            response.UserId.Should().Be(_newAdUserAccount.UserId);
            response.OneTimePassword.Should().Be(_newAdUserAccount.OneTimePassword);
            IdentityServiceApiClient.Verify(i => i.CreateUserAsync(It.IsAny<string>(), "fName", "lName", "fName lName", RecoveryEmail, false), Times.Once);
        }

        //Recovery email is not a valid email
        [Test]
        public void Should_return_recovery_email_is_not_valid()
        {
            var invalidRecoveryEmail = "email.@email.com";
            Filter = $"otherMails/any(c:c eq '{invalidRecoveryEmail.Replace("'", "''")}')";

            var response = Assert.ThrowsAsync<InvalidEmailException>(async () => await Service.CreateUserAsync("fName", "lName", invalidRecoveryEmail, false));

            response.Message.Should().Be("Recovery email is not a valid email");
        }
    }
}