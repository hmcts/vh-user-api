using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTests.Common.Api.Helpers;
using FluentAssertions;
using TechTalk.SpecFlow;
using Testing.Common.ActiveDirectory;
using Testing.Common.Helpers;
using UserApi.AcceptanceTests.Contexts;
using UserApi.Contract.Requests;
using UserApi.Contract.Responses;
using static Testing.Common.Helpers.UserApiUriFactory.AccountEndpoints;

namespace UserApi.AcceptanceTests.Steps
{
    [Binding]
    public sealed class AccountSteps
    {
        private readonly TestContext _context;
        private const int DelayInMilliseconds = 1000;
        private const int TimeoutInMilliseconds = 20000;

        public AccountSteps(TestContext context)
        {
            _context = context;
        }

        [Given(@"I have a get ad group by name request with a valid group name")]
        public void GivenIHaveAGetAdGroupByNameRequestWithAValidGroupName()
        {
            _context.Request = _context.Get(GetGroupByName(_context.Config.TestSettings.ExistingGroups.First().DisplayName));
        }

        [Given(@"I have a get ad group by id request with a valid group id")]
        public void GivenIHaveAGetAdGroupByIdRequestWithAValidGroupId()
        {
            _context.Request = _context.Get(GetGroupById(_context.Config.TestSettings.ExistingGroups.First().GroupId));
        }

        [Given(@"I have a get ad groups for a user request for a valid user id")]
        public void GivenIHaveAGetAdGroupsForAUserRequestForAValidUserId()
        {
            _context.Request = _context.Get(GetGroupsForUser(_context.Config.TestSettings.ExistingUserId));
        }

        [Given(@"I have an add a user to a group request for a valid user id and valid group")]
        public void GivenIHaveAnAddAUserToAGroupRequestForAValidUserIdAndValidGroup()
        {
            var addUserRequest = new AddUserToGroupRequest()
            {
                UserId = _context.Config.TestSettings.ExistingUserId,
                GroupName = _context.Config.TestSettings.NewGroups.First().DisplayName
            };
            _context.Request = _context.Patch(AddUserToGroup, addUserRequest);
        }

        [Then(@"the ad groups should be retrieved")]
        public void ThenTheAdGroupsShouldBeRetrieved()
        {
            var model = RequestHelper.DeserialiseSnakeCaseJsonToResponse<GroupsResponse>(_context.Response.Content);
            model.Should().NotBeNull();
            model.DisplayName.Should().NotBeNullOrEmpty();
            model.GroupId.Should().NotBeNullOrEmpty();
        }

        [Then(@"a list of ad groups should be retrieved")]
        public void ThenAListOfAdGroupsShouldBeRetrieved()
        {
            var model = RequestHelper.DeserialiseSnakeCaseJsonToResponse<List<GroupsResponse>>(_context.Response.Content);
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
                userIsInTheGroup = await ActiveDirectoryUser.IsUserInAGroupAsync(_context.Config.TestSettings.ExistingUserId,
                    _context.Config.TestSettings.NewGroups.First().DisplayName, _context.Tokens.GraphApiBearerToken);
                await Task.Delay(DelayInMilliseconds);
            }

            sw.Stop();
            userIsInTheGroup.Should().BeTrue("User has been added to the group");
            _context.Test.NewGroupId = _context.Config.TestSettings.NewGroups.First().GroupId;
        }
    }
}
