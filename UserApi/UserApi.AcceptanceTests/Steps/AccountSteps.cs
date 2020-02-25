using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private readonly TestContext _context;
        private readonly AccountEndpoints _endpoints = new ApiUriFactory().AccountEndpoints;
        private const int DelayInMilliseconds = 1000;
        private const int TimeoutInMilliseconds = 20000;

        public AccountSteps(TestContext context)
        {
            _context = context;
        }

        [BeforeScenario]
        public static async Task RemoveNewGroupIfExistsAsync(TestContext testContext)
        {
            await RemoveGroupFromUserIfExists(testContext);
        }

        [Given(@"I have a get ad group by name request with a valid group name")]
        public void GivenIHaveAGetAdGroupByNameRequestWithAValidGroupName()
        {
            _context.Request =
                _context.Get(
                    _endpoints.GetGroupByName(_context.TestSettings.ExistingGroups.First().DisplayName));
        }

        [Given(@"I have a get ad group by id request with a valid group id")]
        public void GivenIHaveAGetAdGroupByIdRequestWithAValidGroupId()
        {
            _context.Request = _context.Get(_endpoints.GetGroupById(_context.TestSettings.ExistingGroups.First().GroupId));
        }

        [Given(@"I have a get ad groups for a user request for a valid user id")]
        public void GivenIHaveAGetAdGroupsForAUserRequestForAValidUserId()
        {
            _context.Request = _context.Get(_endpoints.GetGroupsForUser(_context.TestSettings.ExistingUserId));
        }

        [Given(@"I have an add a user to a group request for a valid user id and valid group")]
        public void GivenIHaveAnAddAUserToAGroupRequestForAValidUserIdAndValidGroup()
        {
            var addUserRequest = new AddUserToGroupRequest()
            {
                UserId = _context.TestSettings.ExistingUserId,
                GroupName = _context.TestSettings.NewGroups.First().DisplayName
            };
            _context.Request = _context.Patch(_endpoints.AddUserToGroup, addUserRequest);
        }

        [Then(@"the ad groups should be retrieved")]
        public void ThenTheAdGroupsShouldBeRetrieved()
        {
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<GroupsResponse>(_context.Json);
            model.Should().NotBeNull();
            model.DisplayName.Should().NotBeNullOrEmpty();
            model.GroupId.Should().NotBeNullOrEmpty();
        }

        [Then(@"a list of ad groups should be retrieved")]
        public void ThenAListOfAdGroupsShouldBeRetrieved()
        {
            var model = ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<List<GroupsResponse>>(_context.Json);
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
            var userIsInTheGroup = false;
            var sw = new Stopwatch();
            sw.Start();
            while (!userIsInTheGroup && sw.ElapsedMilliseconds < TimeoutInMilliseconds)
            {
                userIsInTheGroup = await ActiveDirectoryUser.IsUserInAGroupAsync(_context.TestSettings.ExistingUserId,
                    _context.TestSettings.NewGroups.First().DisplayName, _context.GraphApiToken);
                await Task.Delay(DelayInMilliseconds);
            }

            sw.Stop();
            userIsInTheGroup.Should().BeTrue("User has been added to the group");
            _context.NewGroupId = _context.TestSettings.NewGroups.First().GroupId;
        }

        [AfterScenario]
        public static async Task RemoveNewGroupAgainIfExists(TestContext testContext)
        {
            await RemoveGroupFromUserIfExists(testContext);
            testContext.NewGroupId = null;
        }

        private static async Task RemoveGroupFromUserIfExists(TestContext testContext)
        {
            var userIsInTheGroup = await ActiveDirectoryUser.IsUserInAGroupAsync(testContext.TestSettings.ExistingUserId,
                testContext.TestSettings.NewGroups.First().DisplayName, testContext.GraphApiToken);
            if (userIsInTheGroup)
            {
                await ActiveDirectoryUser.RemoveTheUserFromTheGroupAsync(testContext.TestSettings.ExistingUserId,
                    testContext.TestSettings.NewGroups.First().GroupId, testContext.GraphApiToken);
            }
            testContext.NewGroupId = null;
        }
    }
}
