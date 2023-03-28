using System;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Threading.Tasks;
using UserApi.Caching;
using UserApi.Contract.Responses;
using UserApi.Controllers;
using UserApi.Security;
using UserApi.Services;

namespace UserApi.UnitTests.Controllers
{
    public class HealthCheckControllerTests
    {
        private Mock<IUserAccountService> _userAccountService;
        private Mock<ICache> _distributedCacheMock;
        
        private HealthCheckController _controller;

        [SetUp]
        public void Setup()
        {
            _userAccountService = new Mock<IUserAccountService>();
            _distributedCacheMock = new Mock<ICache>();
            
            _controller = new HealthCheckController(_userAccountService.Object, _distributedCacheMock.Object);

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
            var email = "checkuser@hmcts.net";
            var filter = $"otherMails/any(c:c eq '{email}')";
            var message = "GetUserByFilter unauthorized access to Microsoft Graph";
            var reason = "service not available";
            _userAccountService
                .Setup(x => x.GetUserByFilterAsync(filter))
                .ThrowsAsync(new UserServiceException(message, reason));

            var result = await _controller.Health();

            var typedResult = (ObjectResult)result;
            typedResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            var response = (UserApiHealthResponse)typedResult.Value;
            response.UserAccessHealth.Successful.Should().BeFalse();
            response.UserAccessHealth.ErrorMessage.Should().NotBeNullOrEmpty();
            response.UserAccessHealth.ErrorMessage.Should().Be($"{message}: {reason}");

            response.GroupAccessHealth.Successful.Should().BeTrue();
            response.GroupAccessHealth.ErrorMessage.Should().BeNullOrWhiteSpace();
            response.GroupAccessHealth.Data.Should().BeNullOrEmpty();

            response.HealthCheckSuccessful.Should().BeFalse();
        }

        [Test]
        public async Task Should_return_server_error_when_unable_to_access_groups()
        {
            var message = "";
            var reason = "";
            _userAccountService
                .Setup(x => x.GetGroupByNameAsync(It.IsAny<string>()))
                .ThrowsAsync(new UserServiceException(message, reason));

            var result = await _controller.Health();

            var typedResult = (ObjectResult)result;
            typedResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            var response = (UserApiHealthResponse)typedResult.Value;

            response.UserAccessHealth.Successful.Should().BeTrue();
            response.UserAccessHealth.ErrorMessage.Should().BeNullOrEmpty();
            response.UserAccessHealth.Data.Should().BeNullOrEmpty();

            response.GroupAccessHealth.Successful.Should().BeFalse();
            response.GroupAccessHealth.ErrorMessage.Should().NotBeNullOrEmpty();
            response.GroupAccessHealth.ErrorMessage.Should().Be($"{message}: {reason}");

            response.HealthCheckSuccessful.Should().BeFalse();
        }

        [Test]
        public async Task Should_return_success_for_distributed_cache_health_check()
        {
            _distributedCacheMock
                .Setup(x => x.GetOrAddAsync(It.IsAny<Func<Task<object>>>()))
                .ReturnsAsync(new object());
            
            var result = await _controller.Health();

            var typedResult = (ObjectResult) result;
            typedResult.StatusCode.Should().Be((int) HttpStatusCode.OK);
            var response = (UserApiHealthResponse) typedResult.Value;

            response.DistributedCacheHealth.Successful.Should().BeTrue();
            response.DistributedCacheHealth.ErrorMessage.Should().BeNullOrEmpty();
            response.DistributedCacheHealth.Data.Should().BeNullOrEmpty();

            response.HealthCheckSuccessful.Should().BeTrue();
        }
        
        [Test]
        public async Task Should_return_server_error_when_unable_to_access_distributed_cache()
        {
            _distributedCacheMock
                .Setup(x => x.GetOrAddAsync(It.IsAny<Func<Task<string>>>()))
                .ThrowsAsync(new Exception("some error"));
            
            var result = await _controller.Health();

            var typedResult = (ObjectResult) result;
            typedResult.StatusCode.Should().Be((int) HttpStatusCode.InternalServerError);
            var response = (UserApiHealthResponse) typedResult.Value;

            response.DistributedCacheHealth.Successful.Should().BeFalse();
            response.DistributedCacheHealth.ErrorMessage.Should().Be("some error");
            
            response.HealthCheckSuccessful.Should().BeFalse();
        }

        [Test]
        public async Task Should_return_ok_when_the_service_is_healthy()
        {
            var result = await _controller.Health();

            var typedResult = (ObjectResult)result;
            typedResult.StatusCode.Should().Be((int)HttpStatusCode.OK);

            var response = (UserApiHealthResponse)typedResult.Value;
            response.UserAccessHealth.Successful.Should().BeTrue();
            response.UserAccessHealth.ErrorMessage.Should().BeNullOrWhiteSpace();
            response.UserAccessHealth.Data.Should().BeNullOrEmpty();

            response.GroupAccessHealth.Successful.Should().BeTrue();
            response.GroupAccessHealth.ErrorMessage.Should().BeNullOrWhiteSpace();
            response.GroupAccessHealth.Data.Should().BeNullOrEmpty();

            response.DistributedCacheHealth.Successful.Should().BeTrue();
            response.DistributedCacheHealth.ErrorMessage.Should().BeNullOrWhiteSpace();
            response.DistributedCacheHealth.Data.Should().BeNullOrEmpty();

            response.HealthCheckSuccessful.Should().BeTrue();

            _userAccountService.Verify(u => u.GetGroupByNameAsync("TestGroup"), Times.Once);
            _userAccountService.Verify(u => u.GetUserByFilterAsync(It.IsAny<string>()), Times.Once);
            _distributedCacheMock.Verify(x => x.GetOrAddAsync(It.IsAny<Func<Task<string>>>()));
        }

        [Test]
        public async Task Should_return_server_errors_if_access_to_user_and_group_unsuccessful()
        {
            var email = "checkuser@hmcts.net";
            var filter = $"otherMails/any(c:c eq '{email}')";
            var message = string.Empty;
            var reason = string.Empty;
            _userAccountService
               .Setup(x => x.GetGroupByNameAsync(It.IsAny<string>()))
               .ThrowsAsync(new UserServiceException(message, reason));
            _userAccountService
                .Setup(x => x.GetUserByFilterAsync(filter))
                .ThrowsAsync(new UserServiceException(message, reason));

            var result = await _controller.Health();

            var typedResult = (ObjectResult)result;
            typedResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            var response = (UserApiHealthResponse)typedResult.Value;
            response.UserAccessHealth.Successful.Should().BeFalse();
            response.UserAccessHealth.ErrorMessage.Should().NotBeNullOrEmpty();
            response.UserAccessHealth.ErrorMessage.Should().Be($"{message}: {reason}");

            response.GroupAccessHealth.Successful.Should().BeFalse();
            response.GroupAccessHealth.ErrorMessage.Should().NotBeNullOrWhiteSpace();
            response.GroupAccessHealth.ErrorMessage.Should().Be($"{message}: {reason}");

            response.HealthCheckSuccessful.Should().BeFalse();

        }
    }
}