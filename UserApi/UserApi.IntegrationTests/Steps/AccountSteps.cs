using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AcceptanceTests.Common.Api.Helpers;
using FluentAssertions;
using TechTalk.SpecFlow;
using Testing.Common;
using Testing.Common.ActiveDirectory;
using UserApi.Contract.Requests;
using UserApi.Contract.Responses;
using UserApi.IntegrationTests.Contexts;
using UserApi.IntegrationTests.Helpers;
using static Testing.Common.Helpers.UserApiUriFactory.AccountEndpoints;

namespace UserApi.IntegrationTests.Steps
{
    [Binding]
    public sealed class AccountSteps : BaseSteps
    {
        private readonly TestContext _testContext;

        public AccountSteps(TestContext testContext)
        {
            _testContext = testContext;
        }

        [Given(@"I have a get ad group by name request with a (.*) group name")]
        [Given(@"I have a get ad group by name request with an (.*) group name")]
        public void GivenIHaveAGetAdGroupByNameRequest(Scenario scenario)
        {
            _testContext.HttpMethod = HttpMethod.Get;
             _testContext.Uri = scenario switch
            {
                Scenario.Valid => GetGroupByName(_testContext.Config.TestSettings.ExistingGroups.First().DisplayName),
                Scenario.Nonexistent => GetGroupByName("Does not exist"),
                Scenario.Invalid => GetGroupByName(" "),
                _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
            };
        }

        [Given(@"I have a get ad group by id request with a (.*) group id")]
        [Given(@"I have a get ad group by id request with an (.*) group id")]
        public void GivenIHaveAGetAdGroupByIdRequest(Scenario scenario)
        {
            _testContext.HttpMethod = HttpMethod.Get;
            switch (scenario)
            {
                case Scenario.Valid:
                {
                    _testContext.Uri = GetGroupById(_testContext.Config.TestSettings.ExistingGroups.First().GroupId);
                        break;
                }
                case Scenario.Nonexistent:
                {
                    _testContext.Uri = GetGroupById("Does not exist");
                    break;
                }
                case Scenario.Invalid:
                {
                    _testContext.Uri = GetGroupById(" ");
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
        }

        [Given(@"I have a get ad groups for a user request for a (.*) user id")]
        [Given(@"I have a get ad groups for a user request for an (.*) user id")]
        public void GivenIHaveAGetAdGroupForAUserRequestForTheUser(Scenario scenario)
        {
            _testContext.HttpMethod = HttpMethod.Get;
            switch (scenario)
            {
                case Scenario.Valid:
                {
                    _testContext.Uri = GetGroupsForUser(_testContext.Config.TestSettings.ExistingUserId);
                    break;
                }
                case Scenario.Nonexistent:
                {
                    _testContext.Uri = GetGroupsForUser("Does not exist");
                    break;
                }
                case Scenario.Invalid:
                {
                    _testContext.Uri = GetGroupsForUser(" ");
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
        }

        [Given(@"I have an add a user to a group request for a (.*) user id and valid group")]
        [Given(@"I have an add a user to a group request for an (.*) user id and valid group")]
        public void GivenIHaveAnAddAUserToAGroupRequestForTheUserIdAndGroup(Scenario scenario)
        {
            _testContext.HttpMethod = HttpMethod.Patch;
            _testContext.Uri = AddUserToGroup;
            var addUserRequest = new AddUserToGroupRequest()
            {
                UserId = _testContext.Config.TestSettings.ExistingUserId,
                GroupName = _testContext.Config.TestSettings.ExistingGroups.First().DisplayName
            };
            switch (scenario)
            {
                case Scenario.Valid:
                {
                    break;
                }
                case Scenario.Nonexistent:
                {
                    addUserRequest.UserId = Guid.NewGuid().ToString();
                    break;
                }
                case Scenario.Invalid:
                {
                    addUserRequest.UserId = " ";
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
            var jsonBody = RequestHelper.Serialise(addUserRequest);
            _testContext.HttpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        [Given(@"I have an add a user to a group request for an existing user id and (.*) group")]
        public void GivenIHaveAnAddAUserToAGroupRequestForAnExistingUserIdAndExistingGroup(Scenario scenario)
        {
            _testContext.HttpMethod = HttpMethod.Patch;
            _testContext.Uri = AddUserToGroup;
            var addUserRequest = new AddUserToGroupRequest()
            {
                UserId = _testContext.Config.TestSettings.ExistingUserId,
                GroupName = _testContext.Config.TestSettings.ExistingGroups.First().DisplayName
            };
            switch (scenario)
            {               
                case Scenario.Existing:
                {
                    addUserRequest.GroupName = _testContext.Config.TestSettings.ExistingGroups.First().DisplayName;
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
            var jsonBody = RequestHelper.Serialise(addUserRequest);
            _testContext.HttpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        [Then(@"the ad groups should be retrieved")]
        public async Task ThenTheAdGroupsShouldBeRetrieved()
        {
            var json = await _testContext.ResponseMessage.Content.ReadAsStringAsync();
            var model = RequestHelper.Deserialise<GroupsResponse>(json);
            model.Should().NotBeNull();
            model.DisplayName.Should().Be(_testContext.Config.TestSettings.ExistingGroups.First().DisplayName);
            model.GroupId.Should().Be(_testContext.Config.TestSettings.ExistingGroups.First().GroupId);
        }

        [Then(@"a list of ad groups should be retrieved")]
        public async Task ThenAListOfAdGroupsShouldBeRetrieved()
        {
            var json = await _testContext.ResponseMessage.Content.ReadAsStringAsync();
            var model = RequestHelper.Deserialise<List<GroupsResponse>>(json);
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
            var userIsInTheGroup = await ActiveDirectoryUser.IsUserInAGroupAsync(_testContext.Config.TestSettings.ExistingUserId,
                _testContext.Config.TestSettings.ExistingGroups.First().DisplayName, _testContext.Tokens.GraphApiBearerToken);
            userIsInTheGroup.Should().BeTrue();
            _testContext.Test.NewGroupId = _testContext.Config.TestSettings.ExistingGroups.First().GroupId;
        }

        [AfterScenario]
        public async Task ClearUpAsync(TestContext testContext)
        {
            var newGroup = testContext.Config.TestSettings.ExistingGroups.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(_testContext.Test.NewGroupId) || newGroup == null)
            {
                return;
            }

            await RemoveGroupFromUserIfExistsAsync(testContext, newGroup);

            testContext.Test.NewGroupId = null;
        }

        private static async Task RemoveGroupFromUserIfExistsAsync(TestContext testContext, Group group)
        {
            var userId = testContext.Config.TestSettings.ExistingUserId;
            var userIsInTheGroup = await ActiveDirectoryUser.IsUserInAGroupAsync(userId, group.DisplayName, testContext.Tokens.GraphApiBearerToken);
            if (userIsInTheGroup)
            {
                await ActiveDirectoryUser.RemoveTheUserFromTheGroupAsync(userId, group.GroupId, testContext.Tokens.GraphApiBearerToken);
            }
            testContext.Test.NewGroupId = null;
        }
    }
}
