using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using TechTalk.SpecFlow;
using Testing.Common.ActiveDirectory;
using Testing.Common.Helpers;
using UserApi.Contract.Requests;
using UserApi.Contract.Responses;
using UserApi.IntegrationTests.Contexts;
using UserApi.IntegrationTests.Helpers;

namespace UserApi.IntegrationTests.Steps
{
    [Binding]
    public sealed class AccountSteps : BaseSteps
    {
        private readonly ApiTestContext _apiTestContext;
        private readonly AccountEndpoints _endpoints = new ApiUriFactory().AccountEndpoints;

        public AccountSteps(ApiTestContext apiTestContext)
        {
            _apiTestContext = apiTestContext;
        }

        [Given(@"I have a get ad group by name request with a (.*) group name")]
        [Given(@"I have a get ad group by name request with an (.*) group name")]
        public void GivenIHaveAGetAdGroupByNameRequest(Scenario scenario)
        {
            _apiTestContext.HttpMethod = HttpMethod.Get;
            switch (scenario)
            {
                case Scenario.Valid:
                {
                    _apiTestContext.Uri = _endpoints.GetGroupByName(_apiTestContext.TestSettings.ExistingGroups.First().DisplayName);
                        break;
                }               
                case Scenario.Nonexistent:
                {
                    _apiTestContext.Uri = _endpoints.GetGroupByName("Does not exist");
                    break;
                }
                case Scenario.Invalid:
                {
                    _apiTestContext.Uri = _endpoints.GetGroupByName(" ");
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
        }

        [Given(@"I have a get ad group by id request with a (.*) group id")]
        [Given(@"I have a get ad group by id request with an (.*) group id")]
        public void GivenIHaveAGetAdGroupByIdRequest(Scenario scenario)
        {
            _apiTestContext.HttpMethod = HttpMethod.Get;
            switch (scenario)
            {
                case Scenario.Valid:
                {
                    _apiTestContext.Uri = _endpoints.GetGroupById(_apiTestContext.TestSettings.ExistingGroups.First().GroupId);
                        break;
                }
                case Scenario.Nonexistent:
                {
                    _apiTestContext.Uri = _endpoints.GetGroupById("Does not exist");
                    break;
                }
                case Scenario.Invalid:
                {
                    _apiTestContext.Uri = _endpoints.GetGroupById(" ");
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
        }

        [Given(@"I have a get ad groups for a user request for a (.*) user id")]
        [Given(@"I have a get ad groups for a user request for an (.*) user id")]
        public void GivenIHaveAGetAdGroupForAUserRequestForTheUser(Scenario scenario)
        {
            _apiTestContext.HttpMethod = HttpMethod.Get;
            switch (scenario)
            {
                case Scenario.Valid:
                {
                    _apiTestContext.Uri = _endpoints.GetGroupsForUser(_apiTestContext.TestSettings.ExistingUserId);
                    break;
                }
                case Scenario.Nonexistent:
                {
                    _apiTestContext.Uri = _endpoints.GetGroupsForUser("Does not exist");
                    break;
                }
                case Scenario.Invalid:
                {
                    _apiTestContext.Uri = _endpoints.GetGroupsForUser(" ");
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
        }

        [Given(@"I have an add a user to a group request for a (.*) user id and valid group")]
        [Given(@"I have an add a user to a group request for an (.*) user id and valid group")]
        public void GivenIHaveAnAddAUserToAGroupRequestForTheUserIdAndGroup(Scenario scenario)
        {
            _apiTestContext.HttpMethod = HttpMethod.Patch;
            _apiTestContext.Uri = _endpoints.AddUserToGroup;
            var addUserRequest = new AddUserToGroupRequest()
            {
                UserId = _apiTestContext.TestSettings.ExistingUserId,
                GroupName = _apiTestContext.TestSettings.NewGroups.First().DisplayName
            };
            switch (scenario)
            {
                case Scenario.Valid:
                {
                    break;
                }
                case Scenario.Nonexistent:
                {
                    addUserRequest.UserId = "Does not exist";
                    break;
                }
                case Scenario.Invalid:
                {
                    addUserRequest.UserId = " ";
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
            var jsonBody = ApiRequestHelper.SerialiseRequestToSnakeCaseJson(addUserRequest);
            _apiTestContext.HttpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        [Given(@"I have an add a user to a group request for an existing user id and (.*) group")]
        public void GivenIHaveAnAddAUserToAGroupRequestForAnExistingUserIdAndExistingGroup(Scenario scenario)
        {
            _apiTestContext.HttpMethod = HttpMethod.Patch;
            _apiTestContext.Uri = _endpoints.AddUserToGroup;
            var addUserRequest = new AddUserToGroupRequest()
            {
                UserId = _apiTestContext.TestSettings.ExistingUserId,
                GroupName = _apiTestContext.TestSettings.NewGroups.First().DisplayName
            };
            switch (scenario)
            {               
                case Scenario.Existing:
                {
                    addUserRequest.GroupName = _apiTestContext.TestSettings.ExistingGroups.First().DisplayName;
                    break;
                }
                case Scenario.Nonexistent:
                {
                    addUserRequest.GroupName = "Does not exist";
                    break;
                }
                case Scenario.Invalid:
                {
                    addUserRequest.GroupName = " ";
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
            var jsonBody = ApiRequestHelper.SerialiseRequestToSnakeCaseJson(addUserRequest);
            _apiTestContext.HttpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        [Then(@"the ad groups should be retrieved")]
        public async Task ThenTheAdGroupsShouldBeRetrieved()
        {
            var json = await _apiTestContext.ResponseMessage.Content.ReadAsStringAsync();
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<GroupsResponse>(json);
            model.Should().NotBeNull();
            model.DisplayName.Should().Be(_apiTestContext.TestSettings.ExistingGroups.First().DisplayName);
            model.GroupId.Should().Be(_apiTestContext.TestSettings.ExistingGroups.First().GroupId);
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
        public async Task ThenUserShouldBeAddedToTheGroup()
        {
            var userIsInTheGroup = await ActiveDirectoryUser.IsUserInAGroup(_apiTestContext.TestSettings.ExistingUserId,
                _apiTestContext.TestSettings.NewGroups.First().DisplayName, _apiTestContext.GraphApiToken);
            userIsInTheGroup.Should().BeTrue();
            _apiTestContext.NewGroupId = _apiTestContext.TestSettings.NewGroups.First().GroupId;
        }

        [AfterScenario]
        public async Task ClearUp(ApiTestContext testContext)
        {
            if (string.IsNullOrWhiteSpace(_apiTestContext.NewGroupId)) return;
            await RemoveGroupFromUserIfExists(testContext);
            testContext.NewGroupId = null;
        }

        private static async Task RemoveGroupFromUserIfExists(ApiTestContext testContext)
        {
            var userIsInTheGroup = await ActiveDirectoryUser.IsUserInAGroup(testContext.TestSettings.ExistingUserId,
                testContext.TestSettings.NewGroups.First().DisplayName, testContext.GraphApiToken);
            if (userIsInTheGroup)
            {
                await ActiveDirectoryUser.RemoveTheUserFromTheGroup(testContext.TestSettings.ExistingUserId,
                    testContext.TestSettings.NewGroups.First().GroupId, testContext.GraphApiToken);
            }
            testContext.NewGroupId = null;
        }
    }
}
