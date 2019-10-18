using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Testing.Common.Helpers;
using UserApi.Contract.Responses;

namespace UserApi.IntegrationTests.Controllers
{
    public class AccountController : ControllerTestsBase
    {
        private readonly AccountEndpoints _accountEndpoints = new ApiUriFactory().AccountEndpoints;
        private string _newUserId;

        [Test]
        public async Task Should_get_group_by_name_not_found_with_bogus_group_name()
        {
            var groupName = "foo";
            var getResponse = await SendGetRequestAsync(_accountEndpoints.GetGroupByName(groupName));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task Should_get_group_by_name()
        {
            var groupName = "External";
            var getResponse = await SendGetRequestAsync(_accountEndpoints.GetGroupByName(groupName));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var groupResponseModel =
                ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<GroupsResponse>(getResponse.Content
                    .ReadAsStringAsync().Result);
            groupResponseModel.DisplayName.Should().Be(groupName);
            groupResponseModel.GroupId.Should().NotBeNullOrWhiteSpace();
        }

        [Test]
        public async Task Should_get_group_by_id()
        {
            var groupId = "121fa058-1796-4531-a9ee-63be1d4dc630";
            var getResponse = await SendGetRequestAsync(_accountEndpoints.GetGroupById(groupId));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var groupResponseModel =
                ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<GroupsResponse>(getResponse.Content
                    .ReadAsStringAsync().Result);

            Assert.AreEqual("External", groupResponseModel.DisplayName);
        }

        [Test]
        public async Task Should_get_group_by_id_not_found_with_bogus_id()
        {
            var groupId = Guid.Empty.ToString();
            var getResponse = await SendGetRequestAsync(_accountEndpoints.GetGroupById(groupId));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task Should_get_groups_for_user()
        {
            // user id for Automation01Administrator01
            const string userId = "9a435325-df6d-4937-9f37-baca2052dd5d";
            var getResponse = await SendGetRequestAsync(_accountEndpoints.GetGroupsForUser(userId));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var groupsForUserModel =
                ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<List<GroupsResponse>>(getResponse.Content
                    .ReadAsStringAsync().Result);

            const string expectedGroupName = "VirtualRoomHearingAdministrator";
            var group = groupsForUserModel.FirstOrDefault(g => g.DisplayName == expectedGroupName);
            Assert.IsNotNull(group, $"Automation01Administrator01 should have group '{expectedGroupName}'");
        }

        [Test]
        public async Task Should_get_groups_for_user_not_found_with_bogus_user_id()
        {
            var userId = Guid.Empty.ToString();
            var getResponse = await SendGetRequestAsync(_accountEndpoints.GetGroupsForUser(userId));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TearDown]
        public void ClearUp()
        {
            if (string.IsNullOrWhiteSpace(_newUserId)) return;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GraphApiToken);
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get,
                    $@"https://graph.microsoft.com/v1.0/users/{_newUserId}");
                var result = client.SendAsync(httpRequestMessage).Result;
                result.IsSuccessStatusCode.Should().BeTrue($"{_newUserId} should be deleted");
                _newUserId = null;
            }
        }
    }
}
