using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using TechTalk.SpecFlow;
using Testing.Common.Helpers;
using UserApi.Contract.Responses;
using UserApi.IntegrationTests.Contexts;

namespace UserApi.IntegrationTests.Steps
{
    [Binding]
    public sealed class AccountSteps : BaseSteps
    {
        private readonly ApiTestContext _apiTestContext;
        private readonly ScenarioContext _scenarioContext;
        private const string GroupName = "SSPR Enabled";
        private const string GroupId = "8881ea85-e0c0-4a0b-aa9c-979b9f0c05cd";
        private readonly AccountEndpoints _endpoints = new ApiUriFactory().AccountEndpoints;

        public AccountSteps(ScenarioContext scenarioContext, ApiTestContext apiTestContext)
        {
            _scenarioContext = scenarioContext;
            _apiTestContext = apiTestContext;
        }

        [Given(@"I have a get ad group by name request with the group name '(.*)'")]
        public void GivenIHaveAGetAdGroupByNameRequest(string groupName)
        {
            _scenarioContext.Add("GroupName", groupName);
            _scenarioContext.Add("GroupId", GroupId);
            _apiTestContext.HttpMethod = HttpMethod.Get;
            _apiTestContext.Uri = _endpoints.GetGroupByName(groupName);
        }

        [Given(@"I have a get ad group by id request with the group id '(.*)'")]
        public void GivenIHaveAGetAdGroupByIdRequest(string id)
        {
            _scenarioContext.Add("GroupName", GroupName);
            _scenarioContext.Add("GroupId", id);
            _apiTestContext.HttpMethod = HttpMethod.Get;
            _apiTestContext.Uri = _endpoints.GetGroupById(id);
        }

        [Given(@"I have a get ad groups for a user request for the user id '(.*)'")]
        public void GivenIHaveAGetAdGroupForAUserRequestForTheUser(string userId)
        {
            _apiTestContext.HttpMethod = HttpMethod.Get;
            _apiTestContext.Uri = _endpoints.GetGroupsForUser(userId);
        }

        [Given(@"I have an add a user to a group request for the user id '(.*)' and group '(.*)'")]
        public void GivenIHaveAnAddAUserToAGroupRequestForTheUserIdAndGroup(string userId, string groupName)
        {
            ScenarioContext.Current.Pending();
        }

        [Then(@"the ad groups should be retrieved")]
        public async Task ThenTheAdGroupsShouldBeRetrieved()
        {
            var json = await _apiTestContext.ResponseMessage.Content.ReadAsStringAsync();
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<GroupsResponse>(json);
            model.Should().NotBeNull();
            model.DisplayName.Should().Be(_scenarioContext.Get<string>("GroupName"));
            model.GroupId.Should().Be(_scenarioContext.Get<string>("GroupId"));
        }

        [Then(@"a list of ad groups should be retrieved")]
        public async Task ThenAListOfAdGroupsShouldBeRetrieved()
        {
            var json = await _apiTestContext.ResponseMessage.Content.ReadAsStringAsync();
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<List<GroupsResponse>>(json);
            model.Should().NotBeNull();
            foreach (var group in model)
            {
                group.GroupId.Should().NotBeNullOrEmpty();
                group.DisplayName.Should().NotBeNullOrEmpty();
            }
        }

        [Then(@"user should be added to the group")]
        public void ThenUserShouldBeAddedToTheGroup()
        {
            ScenarioContext.Current.Pending();
        }
    }
}
