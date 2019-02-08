using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using UserApi.Services;
using Microsoft.ApplicationInsights;
using UserApi.Controllers;
using UserApi.Contracts.Responses;

namespace UserApi.UnitTests.Controllers
{
    public class UserAccountsControllerTests
    {
        private Mock<IUserAccountService> _userAccountService;
        private UserAccountsController _controller;

        [SetUp]
        public void Setup()
        {
            _userAccountService = new Mock<IUserAccountService>();
            _controller = new UserAccountsController(_userAccountService.Object,new TelemetryClient());
        }

        [Test]
        public void Should_get_user_by_id_from_api()
        {
            const string recoveryMail = "testuser@hmcts.com";
            var userResponse = new Microsoft.Graph.User();
            var response = new UserDetailsResponse();

            _userAccountService.Setup(x => x.GetUserById(recoveryMail)).Returns(userResponse);

            var actionResult = (OkObjectResult)(_controller.GetUserByAdUserId(recoveryMail));
            var actualResponse = (UserDetailsResponse)actionResult.Value;
            actualResponse.DisplayName.Should().BeSameAs(response.DisplayName);
        }

    }
}