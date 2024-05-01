using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph.Models;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Testing.Common.Helpers;
using UserApi.Security;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services.UserAccountService
{
    public class AddUserToGroupAsyncTests: UserAccountServiceTests
    {
        private string _userId;
        private string _groupId;
        private CustomDirectoryObject _customDirectoryObject;
        private DirectoryObject _groupDirectoryObject;
        private string _groupAccessUri;

        [SetUp]
        public void TestIntialize()
        {           
            _userId = "1";
            _groupId = "2";
            _customDirectoryObject = new CustomDirectoryObject
            {
                ObjectDataId = $"{GraphApiSettings.GraphApiBaseUri}v1.0/{GraphApiSettings.TenantId}/directoryObjects/{_userId}"
            };

            _groupDirectoryObject = new DirectoryObject
            {
                Id = $"{GraphApiSettings.GraphApiBaseUri}v1.0/{GraphApiSettings.TenantId}/directoryObjects/{_groupId}",
                AdditionalData = new Dictionary<string, object>
                {
                    {"value", JsonConvert.SerializeObject(new[] {_groupId})}
                }
            };

            _groupAccessUri = $"{GraphApiSettings.GraphApiBaseUri}v1.0/{GraphApiSettings.TenantId}/groups/{_groupId}/members/$ref"; 
        }

        [Test]
        public async Task Should_add_user_to_group_successfully()
        {
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage("Not Found", HttpStatusCode.NotFound));
            SecureHttpRequest.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<StringContent>(), It.IsAny<string>()))
               .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage("Success", HttpStatusCode.OK));

            await Service.AddUserToGroupAsync(_userId, _groupId);

            SecureHttpRequest.Verify(s => s.PostAsync(It.IsAny<string>(), It.Is<StringContent>(s => s.ReadAsStringAsync().Result == JsonConvert.SerializeObject(_customDirectoryObject)), _groupAccessUri), Times.Once);
        }

        [Test]
        public async Task Should_verify_if_user_already_exists()
        {
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(_groupDirectoryObject, HttpStatusCode.OK));
            SecureHttpRequest.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<StringContent>(), It.IsAny<string>()))
               .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage("already exist", HttpStatusCode.NotFound));

            await Service.AddUserToGroupAsync(_userId, _groupId);

            SecureHttpRequest.Verify(s => s.PostAsync(It.IsAny<string>(), It.Is<StringContent>(s => s.ReadAsStringAsync().Result == JsonConvert.SerializeObject(_customDirectoryObject)), _groupAccessUri), Times.Once);
        }

        [Test]
        public void Should_throw_user_exception_on_other_responses()
        {
            var message = $"Failed to add user {_userId} to group {_groupId}";
            var reason = "Unathorized access";

            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(_groupDirectoryObject, HttpStatusCode.OK));
            SecureHttpRequest.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<StringContent>(), It.IsAny<string>()))
               .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(reason, HttpStatusCode.Unauthorized));

            var response = Assert.ThrowsAsync<UserServiceException>(async () => await Service.AddUserToGroupAsync(_userId, _groupId));

            response.Should().NotBeNull();
            response.Message.Should().Be($"{message}: {reason}");
            response.Reason.Should().Be(reason);
        }
    }
}
