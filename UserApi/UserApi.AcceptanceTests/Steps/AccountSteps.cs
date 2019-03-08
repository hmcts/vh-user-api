using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using TechTalk.SpecFlow;
using Testing.Common.Helpers;
using UserApi.AcceptanceTests.Contexts;
using UserApi.Contract.Requests;
using UserApi.Contract.Responses;

namespace UserApi.AcceptanceTests.Steps
{
    [Binding]
    public sealed class AccountSteps : BaseSteps
    {
        private readonly ScenarioContext _context;
        private readonly AcTestContext _acTestContext;
        private readonly AccountEndpoints _endpoints = new ApiUriFactory().AccountEndpoints;

        public AccountSteps(ScenarioContext injectedContext, AcTestContext acTestContext)
        {
            _context = injectedContext;
            _acTestContext = acTestContext;
        }

        [BeforeScenario("AddGroup")]
        public static void RemoveNewGroupIfExists(AcTestContext testContext)
        {
            // Check the db to see if the user has 
            // _acTestContext.TestSettings.NewGroups.First().DisplayName
            // Remove if so
        }

        [Given(@"I have a get ad group by name request with a valid group name")]
        public void GivenIHaveAGetAdGroupByNameRequestWithAValidGroupName()
        {
            _acTestContext.Request =
                _acTestContext.Get(
                    _endpoints.GetGroupByName(_acTestContext.TestSettings.ExistingGroups.First().DisplayName));
        }

        [Given(@"I have a get ad group by id request with a valid group id")]
        public void GivenIHaveAGetAdGroupByIdRequestWithAValidGroupId()
        {
            _acTestContext.Request =
                _acTestContext.Get(_endpoints.GetGroupById(_acTestContext.TestSettings.ExistingGroups.First().GroupId));
        }

        [Given(@"I have a get ad groups for a user request for a valid user id")]
        public void GivenIHaveAGetAdGroupsForAUserRequestForAValidUserId()
        {
            _acTestContext.Request =
                _acTestContext.Get(_endpoints.GetGroupsForUser(_acTestContext.TestSettings.ExistingUserId));
        }

        [Given(@"I have an add a user to a group request for a valid user id and valid group")]
        public void GivenIHaveAnAddAUserToAGroupRequestForAValidUserIdAndValidGroup()
        {
            var addUserRequest = new AddUserToGroupRequest()
            {
                UserId = _acTestContext.TestSettings.ExistingUserId,
                GroupName = _acTestContext.TestSettings.NewGroups.First().DisplayName
            };
            _acTestContext.Request = _acTestContext.Patch(_endpoints.AddUserToGroup, addUserRequest);
        }

        [Then(@"the ad groups should be retrieved")]
        public void ThenTheAdGroupsShouldBeRetrieved()
        {
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<GroupsResponse>(_acTestContext.Json);
            model.Should().NotBeNull();
            model.DisplayName.Should().NotBeNullOrEmpty();
            model.GroupId.Should().NotBeNullOrEmpty();
        }

        [Then(@"a list of ad groups should be retrieved")]
        public void ThenAListOfAdGroupsShouldBeRetrieved()
        {
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<List<GroupsResponse>>(_acTestContext.Json);
            model.Should().NotBeNull();
            foreach (var group in model)
            {
                group.DisplayName.Should().NotBeNullOrEmpty();
                group.GroupId.Should().NotBeNullOrEmpty();
            }
        }

        [Then(@"user should be added to the group")]
        public void ThenUserShouldBeAddedToTheGroup()
        {
            // Check the db to see if the user has 
            // _acTestContext.TestSettings.NewGroups.First().DisplayName        }
        }

        [AfterScenario("AddGroup")]
        public static void RemoveNewGroupAgainIfExists(AcTestContext testContext)
        {
            // Check the db to see if the user has 
            // _acTestContext.TestSettings.NewGroups.First().DisplayName
            // Remove if so
        }
    }
}
