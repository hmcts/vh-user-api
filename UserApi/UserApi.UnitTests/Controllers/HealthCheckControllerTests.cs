using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Moq;
using NUnit.Framework;
using UserApi.Contract.Responses;
using UserApi.Controllers;
using UserApi.Services;
using System.Net;
using UserApi.Security;

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
            _controller = new HealthCheckController(_userAccountService.Object, null);
        }

        [Test]
        public async Task should_return_server_error_when_the_service_is_unhealthy()
        {
            var email = "checkuser@test.com";
            var filter = $"otherMails/any(c:c eq '{email}')";
            var message = "Could not retrieve ref data during service health check";
            var reason = "service not available";
            _userAccountService
                .Setup(x => x.GetUserByFilter(filter))
                .Throws(new UserServiceException(message, reason));

            var result = await _controller.Health();
            var objectResult = (ObjectResult)result;

            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            objectResult.Value.ToString().Should().Contain("Could not retrieve ref data during service health check");
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