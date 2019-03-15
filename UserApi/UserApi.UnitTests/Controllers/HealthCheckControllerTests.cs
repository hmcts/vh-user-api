using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Threading.Tasks;
using UserApi.Controllers;
using UserApi.Security;
using UserApi.Services;

namespace UserApi.UnitTests.Controllers
{
    public class HealthCheckControllerTests
    {
        private HealthCheckController _controller;
        private Mock<IUserAccountService> _userAccountService;

        [SetUp]
        public void Setup()
        {
            _userAccountService = new Mock<IUserAccountService>();
            _controller = new HealthCheckController(_userAccountService.Object);
        }

        [Test]
        public async Task should_return_server_error_when_the_service_is_unhealthy()
        {
            var email = "checkuser@test.com";
            var filter = $"otherMails/any(c:c eq '{email}')";
            var message = "GetUserByFilter unauthorized access to Microsoft Graph";
            var reason = "service not available";
            _userAccountService
                .Setup(x => x.GetUserByFilter(filter))
                .Throws(new UserServiceException(message, reason));
            var result = await _controller.Health();
            var objectResult = (ObjectResult)result;

            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        }

        [Test]
        public async Task should_return_ok_when_the_service_is_healthy()
        {
            var email = "checkuser@test.com";
            var filter = $"otherMails/any(c:c eq '{email}')";

            _userAccountService
                .Setup(x => x.GetUserByFilter(filter))
                .ReturnsAsync(new User());

            var result = await _controller.Health();

            result.Should().NotBeNull();
        }
    }
}