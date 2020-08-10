using FluentAssertions;
using Microsoft.Graph;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UserApi.Caching;
using UserApi.Common;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services.UserAccountService
{
    public class UserAccountServiceTests
    {
        protected const string Domain = "@hearings.test.server.net";

        protected Mock<ISecureHttpRequest> SecureHttpRequest;
        protected GraphApiSettings GraphApiSettings;
        protected Mock<IIdentityServiceApiClient> IdentityServiceApiClient;
        protected UserApi.Services.UserAccountService Service;
        protected AzureAdGraphUserResponse AzureAdGraphUserResponse;
        protected AzureAdGraphQueryResponse<AzureAdGraphUserResponse> AzureAdGraphQueryResponse;
        private Settings _settings;
        protected string Filter;
        protected DirectoryObject DirectoryObject;
        protected Mock<ICache> DistributedCache;

        [SetUp]
        public void Setup()
        {
            SecureHttpRequest = new Mock<ISecureHttpRequest>();

            _settings = new Settings { IsLive = true, ReformEmail = Domain.Replace("@", "") };

            var azureAdConfig = new AzureAdConfiguration()
            {
                ClientId = "TestClientId",
                ClientSecret = "TestSecret",
                Authority = "https://Test/Authority",
            };
            var tokenProvider = new Mock<ITokenProvider>();
            GraphApiSettings = new GraphApiSettings(tokenProvider.Object, azureAdConfig);
            IdentityServiceApiClient = new Mock<IIdentityServiceApiClient>();

            AzureAdGraphUserResponse = new AzureAdGraphUserResponse()
            {
                ObjectId = "1",
                DisplayName = "T Tester",
                GivenName = "Test",
                Surname = "Tester",
                OtherMails = new List<string>(),
                UserPrincipalName = "TestUser"
            };
            AzureAdGraphQueryResponse = new AzureAdGraphQueryResponse<AzureAdGraphUserResponse> { Value = new List<AzureAdGraphUserResponse> { AzureAdGraphUserResponse } };

            var additionalData = new Dictionary<string, object>();

            var jobject = new JObject
            {
                new JProperty("@odata.type","#microsoft.graph.group"),
                new JProperty("startDateTime",DateTime.UtcNow),
                new JProperty("endDateTime", DateTime.UtcNow.AddYears(1)),
                new JProperty("secretText", "The passwords must be 16-64 characters in length"),
                new JProperty("keyId", Guid.NewGuid().ToString()),
                new JProperty("hint", "something")
            };

            additionalData.Add("value", new List<JObject> { jobject });
            DirectoryObject = new DirectoryObject
            {
                Id = "1",
                AdditionalData = additionalData,
                ODataType = "@odata.type"
            };
            
            DistributedCache = new Mock<ICache>();

            Service = new UserApi.Services.UserAccountService(SecureHttpRequest.Object, GraphApiSettings, IdentityServiceApiClient.Object, _settings, DistributedCache.Object);
        }

        protected string AccessUri => $"{GraphApiSettings.GraphApiBaseUriWindows}{GraphApiSettings.TenantId}/users?$filter={Filter}&api-version=1.6";
        

        [Test]
        public async Task Should_increment_the_username()
        {
            const string firstName = "Existing";
            const string lastName = "User";
            var baseUsername = $"{firstName}.{lastName}".ToLowerInvariant();

            // given api returns
            var existingUsers = new[] { "existing.user", "existing.user1" };
            IdentityServiceApiClient.Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUsers.Select(username => username + Domain));

            var nextAvailable = await Service.CheckForNextAvailableUsernameAsync(firstName, lastName);

            nextAvailable.Should().Be("existing.user2" + Domain);
            IdentityServiceApiClient.Verify(i => i.GetUsernamesStartingWithAsync(baseUsername), Times.Once);
        }

        [Test]
        public async Task Should_delete()
        {
            IdentityServiceApiClient.Setup(x => x.DeleteUserAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await Service.DeleteUserAsync("User");

            IdentityServiceApiClient.Verify(i => i.DeleteUserAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task Should_update_user_data_in_aad()
        {
            IdentityServiceApiClient.Setup(x => x.UpdateUserAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await Service.UpdateUserAsync("known.user@gmail.com");

            IdentityServiceApiClient.Verify(i => i.UpdateUserAsync(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Should_get_judges_except_test_judges()
        {
            var judgesList = new List<UserResponse>{ new UserResponse { Email = "aa@aa.aa" },
            new UserResponse { Email = "bb@aa.aa" }, new UserResponse{Email="test@aa.aa"} };

            var testJudges = new List<UserResponse> { new UserResponse { Email = "test@aa.aa" } };

            var result = judgesList.Except(testJudges, UserApi.Services.UserAccountService.CompareJudgeById).ToList();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("aa@aa.aa", result[0].Email);
            Assert.AreEqual("bb@aa.aa", result[1].Email);

        }

        [Test]
        public async Task Should_filter_users()
        {
            var serialised = JsonConvert.SerializeObject(AzureAdGraphQueryResponse);
            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(serialised);
            response.Content.Headers.Clear();
            response.Content.Headers.Add("Content-Type", "application/json");
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            var result = await Service.GetUserByFilterAsync("user@test.aa");

            Assert.IsNotNull(result);
            Assert.AreEqual("1", result.Id);
            Assert.AreEqual("T Tester", result.DisplayName);
            Assert.AreEqual("Test", result.GivenName);
            Assert.AreEqual("Tester", result.Surname);
            Assert.AreEqual("TestUser", result.UserPrincipalName);
        }

        [Test]
        public async Task Should_filter_users_and_return_null_if_no_users()
        {
            AzureAdGraphQueryResponse = new AzureAdGraphQueryResponse<AzureAdGraphUserResponse>();
            var serialised = JsonConvert.SerializeObject(AzureAdGraphQueryResponse);
            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(serialised);
            response.Content.Headers.Clear();
            response.Content.Headers.Add("Content-Type", "application/json");
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            var result = await Service.GetUserByFilterAsync("user@test.aa");

            Assert.IsNull(result);
        }

        [Test]
        public async Task Should_get_group_for_user()
        {
            var serialised = JsonConvert.SerializeObject(DirectoryObject);
            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(serialised);
            response.Content.Headers.Clear();
            response.Content.Headers.Add("Content-Type", "application/json");
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            var result = await Service.GetGroupsForUserAsync("user@test.aa");

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public async Task Should_get_empty_group_for_user_with_two_records_for_object_type()
        {
            var additionalData = new Dictionary<string, object>();
            var jobject = new JObject
            {
                new JProperty("dif",DateTime.UtcNow),
            };

            additionalData.Add("value", new List<JObject> { jobject });
            var directoryObject = new DirectoryObject
            {
                Id = "1",
                AdditionalData = additionalData,
                ODataType = "@odata.type"
            };

            var serialised = JsonConvert.SerializeObject(directoryObject);
            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(serialised);
            response.Content.Headers.Clear();
            response.Content.Headers.Add("Content-Type", "application/json");
            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            var result = await Service.GetGroupsForUserAsync("user@test.aa");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task Should_check_for_next_available_user_name_with_no_prefix()
        {
            IdentityServiceApiClient.Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "adam.green" });
            var result = await Service.CheckForNextAvailableUsernameAsync("Adam","Green");

            Assert.IsTrue(result.Contains("adam.green@hearings.test.server.net"));
        }

        [Test]
        public async Task Should_check_for_next_available_user_name_find_with_prefix()
        {
            IdentityServiceApiClient.Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "adam.green2@hearings.test.server.net", "adam.green@hearings.test.server.net", "adam.green1@hearings.test.server.net" });
            var result = await Service.CheckForNextAvailableUsernameAsync("Adam", "Green");

            Assert.IsTrue(result.Contains("adam.green3@hearings.test.server.net"));
        }

        [Test]
        public async Task Should_check_for_next_available_user_name_find_null()
        {
            IdentityServiceApiClient.Setup(x => x.GetUsernamesStartingWithAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<string> ());
            var result = await Service.CheckForNextAvailableUsernameAsync("Adam", "Green");

            Assert.IsTrue(result.Contains("adam.green@hearings.test.server.net"));
        }
    }
}
