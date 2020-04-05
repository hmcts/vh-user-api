using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Testing.Common.Helpers;
using UserApi.Security;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services.UserAccountService
{
    public class GetUserByFilterAsyncTests: UserAccountServiceTests
    {
        [SetUp]
        public void TestInitialize()
        {
            filter = "test";
        }


        [Test]
        public async Task Should_return_user_by_given_filter()
        {
            azureAdGraphQueryResponse.Value.Add(new AzureAdGraphUserResponse() { ObjectId = "2" });

            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(azureAdGraphQueryResponse, HttpStatusCode.OK));

            var response = await _service.GetUserByFilterAsync(filter);

            response.Should().NotBeNull();
            response.Id.Should().Be(azureAdGraphUserResponse.ObjectId);
            response.DisplayName.Should().Be(azureAdGraphUserResponse.DisplayName);
            response.GivenName.Should().Be(azureAdGraphUserResponse.GivenName);
            response.Surname.Should().Be(azureAdGraphUserResponse.Surname);
            response.Mail.Should().BeNull();
            response.UserPrincipalName.Should().Be(azureAdGraphUserResponse.UserPrincipalName);
        }
        
        [Test]
        public async Task Should_return_null_with_no_matching_user_filter()
        {
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage("NotFound", HttpStatusCode.NotFound));

            var response = await _service.GetUserByFilterAsync(filter);

            response.Should().BeNull();
        }

        [Test]
        public void Should_return_user_exception_for_other_responses()
        { 
            const string message = "User not authorised";

            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(message, HttpStatusCode.Unauthorized));

            var response = Assert.ThrowsAsync<UserServiceException>(async () => await _service.GetUserByFilterAsync(filter));

            response.Should().NotBeNull();
            response.Message.Should().Be($"Failed to search user with filter test: {message}");
            response.Reason.Should().Be(message);
        }
    }
}
