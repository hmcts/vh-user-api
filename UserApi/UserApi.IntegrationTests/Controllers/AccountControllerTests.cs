using System.Net;
using System.Net.Http.Headers;
using NUnit.Framework;
using Testing.Common.Configuration;
using UserApi.Contract.Responses;
using static Testing.Common.Helpers.UserApiUriFactory.AccountEndpoints;

namespace UserApi.IntegrationTests.Controllers
{
    public class AccountController : ControllerTestsBase
    {
        private string _newUserId;

        [Test]
        public async Task Should_get_group_by_name_not_found_with_bogus_group_name()
        {
            var groupName = Guid.NewGuid().ToString();
            var getResponse = await SendGetRequestAsync(GetGroupByName(groupName));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task Should_get_group_by_name()
        {
            var groupName = nameof(TestConfig.Instance.Settings.AdGroup.External);
            var getResponse = await SendGetRequestAsync(GetGroupByName(groupName));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var groupResponseModel = ApiRequestHelper.Deserialise<GroupsResponse>(getResponse.Content
                    .ReadAsStringAsync().Result);
            groupResponseModel.DisplayName.Should().Be(groupName);
            groupResponseModel.GroupId.Should().NotBeNullOrWhiteSpace();
        }

        [Test]
        public async Task Should_get_group_by_id()
        {
            var groupId = TestConfig.Instance.TestSettings.ExistingGroups[0].GroupId;
            var groupName = TestConfig.Instance.TestSettings.ExistingGroups[0].DisplayName;
            var getResponse = await SendGetRequestAsync(GetGroupById(groupId));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var groupResponseModel = ApiRequestHelper.Deserialise<GroupsResponse>(getResponse.Content
                    .ReadAsStringAsync().Result);

            Assert.That(groupName, Is.EqualTo(groupResponseModel.DisplayName));
        }

        [Test]
        public async Task Should_get_group_by_id_not_found_with_bogus_id()
        {
            var groupId = Guid.Empty.ToString();
            var getResponse = await SendGetRequestAsync(GetGroupById(groupId));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task Should_get_groups_for_user()
        {
            string userId = TestConfig.Instance.TestSettings.ExistingUserId;
            var getResponse = await SendGetRequestAsync(GetGroupsForUser(userId));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var groupsForUserModel = ApiRequestHelper.Deserialise<List<GroupsResponse>>(getResponse.Content
                    .ReadAsStringAsync().Result);

            const string expectedGroupName = "External";
            var group = groupsForUserModel.Find(g => g.DisplayName == expectedGroupName);
            Assert.That(group, Is.Not.Null, $"User should have group '{expectedGroupName}'");
        }

        [Test]
        public async Task Should_get_groups_for_user_not_found_with_bogus_user_id()
        {
            var userId = Guid.Empty.ToString();
            var getResponse = await SendGetRequestAsync(GetGroupsForUser(userId));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TearDown]
        public void ClearUp()
        {
            if (string.IsNullOrWhiteSpace(_newUserId)) return;
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GraphApiToken);
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get,
                $@"https://graph.microsoft.com/v1.0/users/{_newUserId}");
            var result = client.SendAsync(httpRequestMessage).Result;
            result.IsSuccessStatusCode.Should().BeTrue($"{_newUserId} should be deleted");
            _newUserId = null;
        }
    }
}
