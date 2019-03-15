using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Moq;
using NUnit.Framework;
using UserApi.Controllers;
using UserApi.Services;
using UserApi.Services.Models;
namespace UserApi.UnitTests.Controllers
{
    public class UserAccountsControllerTests
    {
        private UserController _controller;
        private Mock<IUserAccountService> _userAccountService;

        [SetUp]
        public void Setup()
        {
            _userAccountService = new Mock<IUserAccountService>();
            _controller = new UserController(_userAccountService.Object, new TelemetryClient());
        }

        [Test]
        public async Task Should_get_user_by_user_id_from_api()
        {
            const string userId = "b67d648b-f226-4880-88e1-51b6d1ec7da7";
            var userResponse = new User
            {
                DisplayName = "Sample User",
                GivenName = "User",
                Surname = "Sample"
            };
            var response = new UserProfile
            {
                DisplayName = "Sample User",
                FirstName = "User",
                LastName = "Sample"
            };

            var filter = $"objectId  eq '{userId}'";
            _userAccountService.Setup(x => x.GetUserByFilter(filter)).Returns(Task.FromResult(userResponse));

            var actionResult = (OkObjectResult) await _controller.GetUserByAdUserId(userId);
            var actualResponse = (UserProfile) actionResult.Value;
            actualResponse.DisplayName.Should().BeSameAs(response.DisplayName);
            actualResponse.FirstName.Should().BeSameAs(response.FirstName);
            actualResponse.LastName.Should().BeSameAs(response.LastName);
        }


        [Test]
        public async Task Should_get_user_by_user_name_from_api()
        {
            const string userName = "sample.user@***REMOVED***";
            var userResponse = new User
            {
                DisplayName = "Sample User",
                GivenName = "User",
                Surname = "Sample"
            };
            var response = new UserProfile
            {
                DisplayName = "Sample User",
                FirstName = "User",
                LastName = "Sample"
            };

            var filter = $"userPrincipalName  eq '{userName}'";
            _userAccountService.Setup(x => x.GetUserByFilter(filter)).Returns(Task.FromResult(userResponse));

            var actionResult = (OkObjectResult) await _controller.GetUserByUserName(userName);
            var actualResponse = (UserProfile) actionResult.Value;
            actualResponse.DisplayName.Should().BeSameAs(response.DisplayName);
            actualResponse.FirstName.Should().BeSameAs(response.FirstName);
            actualResponse.LastName.Should().BeSameAs(response.LastName);
        }

        [Test]
        public async Task Should_get_user_by_email_from_api()
        {
            const string email = "sample.user@gmail.com";
            var userResponse = new User
            {
                DisplayName = "Sample User",
                GivenName = "User",
                Surname = "Sample"
            };
            var response = new UserProfile
            {
                DisplayName = "Sample User",
                FirstName = "User",
                LastName = "Sample"
            };

            var filter = $"otherMails/any(c:c eq '{email}')";
            _userAccountService.Setup(x => x.GetUserByFilter(filter)).Returns(Task.FromResult(userResponse));

            var actionResult = (OkObjectResult) await _controller.GetUserByEmail(email);
            var actualResponse = (UserProfile) actionResult.Value;
            actualResponse.DisplayName.Should().BeSameAs(response.DisplayName);
            actualResponse.FirstName.Should().BeSameAs(response.FirstName);
            actualResponse.LastName.Should().BeSameAs(response.LastName);
        }
    }
}