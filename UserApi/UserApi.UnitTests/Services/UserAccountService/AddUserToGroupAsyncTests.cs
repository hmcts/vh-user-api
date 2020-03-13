using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using UserApi.Security;
using UserApi.Services.Models;
using UserApi.UnitTests.Helpers;

namespace UserApi.UnitTests.Services.UserAccountService
{
    public class AddUserToGroupAsyncTests: UserAccountServiceTests
    {
        private Microsoft.Graph.User _user;
        private Microsoft.Graph.Group _group;
        private CustomDirectoryObject _customDirectoryObject;
        private string _groupAccessUri;

        [SetUp]
        public void TestInitialize()
        {           
            _user = new Microsoft.Graph.User() { Id = "1" };
            _group = new Microsoft.Graph.Group() { Id = "2" };
            _customDirectoryObject = new CustomDirectoryObject
            {
                ObjectDataId = $"{GraphApiSettings.GraphApiUri}v1.0/{GraphApiSettings.TenantId}/directoryObjects/{_user.Id}"
            };

            _groupAccessUri = $"{GraphApiSettings.GraphApiUri}v1.0/{GraphApiSettings.TenantId}/groups/{_group.Id}/members/$ref"; 
        }

        [Test]
        public async Task Should_add_user_to_group_successfully()
        {

            SecureHttpRequest.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<StringContent>(), It.IsAny<string>()))
               .ReturnsAsync(RequestHelper.CreateHttpResponseMessage("Success", HttpStatusCode.OK));

            await Service.AddUserToGroupAsync(_user, _group);

            SecureHttpRequest.Verify(s => s.PostAsync(It.IsAny<string>(), It.Is<StringContent>(s => s.ReadAsStringAsync().Result == JsonConvert.SerializeObject(_customDirectoryObject)), _groupAccessUri), Times.Once);
        }

        [Test]
        public async Task Should_verify_if_user_already_exists()
        {

            SecureHttpRequest.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<StringContent>(), It.IsAny<string>()))
               .ReturnsAsync(RequestHelper.CreateHttpResponseMessage("already exist", HttpStatusCode.NotFound));

            await Service.AddUserToGroupAsync(_user, _group);

            SecureHttpRequest.Verify(s => s.PostAsync(It.IsAny<string>(), It.Is<StringContent>(s => s.ReadAsStringAsync().Result == JsonConvert.SerializeObject(_customDirectoryObject)), _groupAccessUri), Times.Once);
        }

        [Test]
        public void Should_throw_user_exception_on_other_responses()
        {
            var message = $"Failed to add user {_user.Id} to group {_group.Id}";
            var reason = "Unathorized access";

            SecureHttpRequest.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<StringContent>(), It.IsAny<string>()))
               .ReturnsAsync(RequestHelper.CreateHttpResponseMessage(reason, HttpStatusCode.Unauthorized));

            var response = Assert.ThrowsAsync<UserServiceException>(async () => await Service.AddUserToGroupAsync(_user, _group));

            response.Should().NotBeNull();
            response.Message.Should().Be($"{message}: {reason}");
            response.Reason.Should().Be(reason);
        }
    }
}
