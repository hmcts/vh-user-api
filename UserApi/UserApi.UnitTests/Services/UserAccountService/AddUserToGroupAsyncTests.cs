using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Testing.Common.Helpers;
using UserApi.Security;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services
{
    public class AddUserToGroupAsyncTests: UserAccountServiceTests
    {
        private Microsoft.Graph.User user;
        private Microsoft.Graph.Group group;
        private CustomDirectoryObject customDirectoryObject;
        private string groupAccessUri;

        [SetUp]
        public void TestIntialize()
        {           
            user = new Microsoft.Graph.User() { Id = "1" };
            group = new Microsoft.Graph.Group() { Id = "2" };
            customDirectoryObject = new CustomDirectoryObject
            {
                ObjectDataId = $"{_graphApiSettings.GraphApiBaseUri}v1.0/{_graphApiSettings.TenantId}/directoryObjects/{user.Id}"
            };

            groupAccessUri = $"{_graphApiSettings.GraphApiBaseUri}v1.0/{_graphApiSettings.TenantId}/groups/{group.Id}/members/$ref"; 
        }

        [Test]
        public async Task should_add_user_to_group_successfully()
        {

            _secureHttpRequest.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<StringContent>(), It.IsAny<string>()))
               .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage("Success", HttpStatusCode.OK));

            await _service.AddUserToGroupAsync(user, group);

            _secureHttpRequest.Verify(s => s.PostAsync(It.IsAny<string>(), It.Is<StringContent>(s => s.ReadAsStringAsync().Result == JsonConvert.SerializeObject(customDirectoryObject)), groupAccessUri), Times.Once);
        }

        [Test]
        public async Task should_verify_if_user_already_exists()
        {

            _secureHttpRequest.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<StringContent>(), It.IsAny<string>()))
               .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage("already exist", HttpStatusCode.NotFound));

            await _service.AddUserToGroupAsync(user, group);

            _secureHttpRequest.Verify(s => s.PostAsync(It.IsAny<string>(), It.Is<StringContent>(s => s.ReadAsStringAsync().Result == JsonConvert.SerializeObject(customDirectoryObject)), groupAccessUri), Times.Once);
        }

        [Test]
        public void should_throw_user_exception_on_other_responses()
        {
            var message = $"Failed to add user {user.Id} to group {group.Id}";
            var reason = "Unathorized access";

            _secureHttpRequest.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<StringContent>(), It.IsAny<string>()))
               .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(reason, HttpStatusCode.Unauthorized));

            var response = Assert.ThrowsAsync<UserServiceException>(async () => await _service.AddUserToGroupAsync(user, group));

            response.Should().NotBeNull();
            response.Message.Should().Be($"{message}: {reason}");
            response.Reason.Should().Be(reason);
        }
    }
}
