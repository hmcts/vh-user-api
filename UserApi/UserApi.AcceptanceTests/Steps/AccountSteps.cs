using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using TechTalk.SpecFlow;
using Testing.Common.ActiveDirectory;
using Testing.Common.Helpers;
using UserApi.AcceptanceTests.Contexts;
using UserApi.Contract.Requests;
using UserApi.Contract.Responses;

namespace UserApi.AcceptanceTests.Steps
{
    [Binding]
    public sealed class AccountSteps : BaseSteps
    {
        private readonly AcTestContext _acTestContext;
        private readonly AccountEndpoints _endpoints = new ApiUriFactory().AccountEndpoints;
        private const int DelayInMilliseconds = 1000;
        private const int TimeoutInMilliseconds = 20000;

        public AccountSteps(AcTestContext acTestContext)
        {
            _acTestContext = acTestContext;
        }

        [BeforeScenario]
        public static async Task RemoveNewGroupIfExistsAsync(AcTestContext testContext)
        {
            await RemoveGroupFromUserIfExists(testContext);
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
        public async Task ThenUserShouldBeAddedToTheGroup()
        {
            var userIsInTheGroup = await ActiveDirectoryUser.IsUserInAGroup(_acTestContext.TestSettings.ExistingUserId,
                _acTestContext.TestSettings.NewGroups.First().DisplayName, _acTestContext.GraphApiToken);
            var sw = new Stopwatch();
            sw.Start();
            while (!userIsInTheGroup && sw.ElapsedMilliseconds < TimeoutInMilliseconds)
            {
                userIsInTheGroup = await ActiveDirectoryUser.IsUserInAGroup(_acTestContext.TestSettings.ExistingUserId,
                    _acTestContext.TestSettings.NewGroups.First().DisplayName, _acTestContext.GraphApiToken);
                await Task.Delay(DelayInMilliseconds);
            }

            sw.Stop();
            userIsInTheGroup.Should().BeTrue("User has been added to the group");
            _acTestContext.NewGroupId = _acTestContext.TestSettings.NewGroups.First().GroupId;
        }

        [AfterScenario]
        public static async Task RemoveNewGroupAgainIfExists(AcTestContext testContext)
        {
            await RemoveGroupFromUserIfExists(testContext);
            testContext.NewGroupId = null;
        }

        private static async Task RemoveGroupFromUserIfExists(AcTestContext testContext)
        {
            var userIsInTheGroup = await ActiveDirectoryUser.IsUserInAGroup(testContext.TestSettings.ExistingUserId,
                testContext.TestSettings.NewGroups.First().DisplayName, testContext.GraphApiToken);
            if (userIsInTheGroup)
            {
                var userRemoved = ActiveDirectoryUser.RemoveTheUserFromTheGroup(testContext.TestSettings.ExistingUserId,
                    testContext.TestSettings.NewGroups.First().GroupId, testContext.GraphApiToken);
                userRemoved.Should().BeTrue($"{testContext.TestSettings.NewGroups.First().DisplayName} is deleted");
            }
            testContext.NewGroupId = null;
        }
    }
}
