using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using NUnit.Framework;
using System.Threading.Tasks;
using UserApi.Contract.Responses;
using FluentAssertions;
using Testing.Common.Helpers;

namespace UserApi.IntegrationTests.Controllers
{
    public class AccountController  : ControllerTestsBase
    {
        private readonly AccountEndpoints _accountEndpoints = new ApiUriFactory().AccountEndpoints;
        private string _newUserId;
        
        [Test]
        public async Task should_get_group_by_name_not_found_with_bogus_group_name()
        {
            var groupName = "foo";
            var getResponse = await SendGetRequestAsync(_accountEndpoints.GetGroupByName(groupName));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task should_get_group_by_name()
        {
            var groupName = "SSPR Enabled";
            var getResponse = await SendGetRequestAsync(_accountEndpoints.GetGroupByName(groupName));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var groupResponseModel =
                ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<GroupsResponse>(getResponse.Content
                    .ReadAsStringAsync().Result);
            groupResponseModel.DisplayName.Should().Be(groupName);
            groupResponseModel.GroupId.Should().NotBeNullOrWhiteSpace();
        }
        
        [Test]
        public async Task should_get_group_by_id()
        {
            var groupId = "8881ea85-e0c0-4a0b-aa9c-979b9f0c05cd";
            var getResponse = await SendGetRequestAsync(_accountEndpoints.GetGroupById(groupId));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var groupResponseModel =
                ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<GroupsResponse>(getResponse.Content
                    .ReadAsStringAsync().Result);
            groupResponseModel.GroupId.Should().Be(groupId);
        }
        
        [Test]
        public async Task should_get_group_by_id_not_found_with_bogus_id()
        {
            var groupId = Guid.Empty.ToString();
            var getResponse = await SendGetRequestAsync(_accountEndpoints.GetGroupById(groupId));
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task should_get_groups_for_user()
        {
            const string userId = "84fa0832-cd70-4788-8f48-e869571e0c56";
            var getResponse = await SendGetRequestAsync(_accountEndpoints.GetGroupsForUser(userId));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var groupsForUserModel =
                ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<List<GroupsResponse>>(getResponse.Content
                    .ReadAsStringAsync().Result);
            groupsForUserModel.Should().NotBeEmpty();
        }

        [Test]
        public async Task should_get_groups_for_user_not_found_with_bogus_user_id()
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