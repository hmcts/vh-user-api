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
            Filter = "test";
        }


        [Test]
        public async Task Should_return_user_by_given_filter()
        {
            AzureAdGraphQueryResponse.Value.Add(new AzureAdGraphUserResponse() { ObjectId = "2" });

            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(AzureAdGraphQueryResponse, HttpStatusCode.OK));

            var response = await Service.GetUserByFilterAsync(Filter);

            response.Should().NotBeNull();
            response.Id.Should().Be(AzureAdGraphUserResponse.ObjectId);
            response.DisplayName.Should().Be(AzureAdGraphUserResponse.DisplayName);
            response.GivenName.Should().Be(AzureAdGraphUserResponse.GivenName);
            response.Surname.Should().Be(AzureAdGraphUserResponse.Surname);
            response.Mail.Should().BeNull();
            response.UserPrincipalName.Should().Be(AzureAdGraphUserResponse.UserPrincipalName);
        }
        
        [Test]
        public async Task Should_return_null_with_no_matching_user_filter()
        {
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage("NotFound", HttpStatusCode.NotFound));

            var response = await Service.GetUserByFilterAsync(Filter);

            response.Should().BeNull();
        }

        [Test]
        public void Should_return_user_exception_for_other_responses()
        { 
            const string message = "User not authorised";

            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(message, HttpStatusCode.Unauthorized));

            var response = Assert.ThrowsAsync<UserServiceException>(async () => await Service.GetUserByFilterAsync(Filter));

            response.Should().NotBeNull();
            response.Message.Should().Be($"Failed to search user with filter test: {message}");
            response.Reason.Should().Be(message);
        }
    }
}
