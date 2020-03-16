using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Threading.Tasks;
using UserApi.Contract.Responses;
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

            _userAccountService
                .Setup(x => x.GetUserByFilterAsync(It.IsAny<string>()))
                .ReturnsAsync(new User());
            
            _userAccountService
                .Setup(x => x.GetGroupByNameAsync(It.IsAny<string>()))
                .ReturnsAsync(new Group());
        }

        [Test]
        public async Task Should_return_server_error_when_unable_to_access_users()
        {
            var email = "checkuser@test.com";
            var filter = $"otherMails/any(c:c eq '{email}')";
            var message = "GetUserByFilter unauthorized access to Microsoft Graph";
            var reason = "service not available";
            _userAccountService
                .Setup(x => x.GetUserByFilterAsync(filter))
                .ThrowsAsync(new UserServiceException(message, reason));
            var result = await _controller.Health();
            
            var typedResult = (ObjectResult) result;
            typedResult.StatusCode.Should().Be((int) HttpStatusCode.InternalServerError);
            var response = (UserApiHealthResponse) typedResult.Value;
            response.UserAccessHealth.Successful.Should().BeFalse();
            response.UserAccessHealth.ErrorMessage.Should().Be($"{message}: {reason} - {reason}");
        }
        
        [Test]
        public async Task Should_return_server_error_when_unable_to_access_groups()
        {
            var message = "GetGroupByName unauthorized access to Microsoft Graph";
            var reason = "service not available";
            _userAccountService
                .Setup(x => x.GetGroupByNameAsync(It.IsAny<string>()))
                .ThrowsAsync(new UserServiceException(message, reason));
            var result = await _controller.Health();
            
            var typedResult = (ObjectResult) result;
            typedResult.StatusCode.Should().Be((int) HttpStatusCode.InternalServerError);
            var response = (UserApiHealthResponse) typedResult.Value;
            response.GroupAccessHealth.Successful.Should().BeFalse();
            response.GroupAccessHealth.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        }

        [Test]
        public async Task Should_return_ok_when_the_service_is_healthy()
        {
            var result = await _controller.Health();

            var typedResult = (ObjectResult) result;
            typedResult.StatusCode.Should().Be((int) HttpStatusCode.OK);
            
            var response = (UserApiHealthResponse) typedResult.Value;
            response.UserAccessHealth.Successful.Should().BeTrue();
            response.UserAccessHealth.ErrorMessage.Should().BeNullOrWhiteSpace();
            response.UserAccessHealth.Data.Should().BeNullOrEmpty();
            
            response.GroupAccessHealth.Successful.Should().BeTrue();
            response.GroupAccessHealth.ErrorMessage.Should().BeNullOrWhiteSpace();
            response.GroupAccessHealth.Data.Should().BeNullOrEmpty();

            _userAccountService.Verify(u => u.GetGroupByNameAsync("TestGroup"), Times.Once);

        }

        [Test]
        public async Task should_return_the_application_version_from_assembly()
        {
            var result = await _controller.Health();
            var typedResult = (ObjectResult)result;
            var response = (UserApiHealthResponse)typedResult.Value;
            response.AppVersion.FileVersion.Should().NotBeNullOrEmpty();
            response.AppVersion.InformationVersion.Should().NotBeNullOrEmpty();
        }
    }
}