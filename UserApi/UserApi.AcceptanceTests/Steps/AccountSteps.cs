using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTests.Common.Api.Requests;
using FluentAssertions;
using TechTalk.SpecFlow;
using Testing.Common.ActiveDirectory;
using UserApi.AcceptanceTests.Contexts;
using UserApi.Contract.Requests;
using UserApi.Contract.Responses;
using static Testing.Common.Helpers.UserApiUriFactory.AccountEndpoints;

namespace UserApi.AcceptanceTests.Steps
{
    [Binding]
    public sealed class AccountSteps
    {
        private readonly TestContext _c;
        private const int DelayInMilliseconds = 1000;
        private const int TimeoutInMilliseconds = 20000;

        public AccountSteps(TestContext context)
        {
            _c = context;
        }

        [Given(@"I have a get ad group by name request with a valid group name")]
        public void GivenIHaveAGetAdGroupByNameRequestWithAValidGroupName()
        {
            var endpoint = GetGroupByName(_c.UserApiConfig.TestConfig.ExistingGroups.First().DisplayName);
            _c.Request = RequestBuilder.Get(endpoint);
        }

        [Given(@"I have a get ad group by id request with a valid group id")]
        public void GivenIHaveAGetAdGroupByIdRequestWithAValidGroupId()
        {
            var endpoint = GetGroupById(_c.UserApiConfig.TestConfig.ExistingGroups.First().GroupId);
            _c.Request = RequestBuilder.Get(endpoint);
        }

        [Given(@"I have a get ad groups for a user request for a valid user id")]
        public void GivenIHaveAGetAdGroupsForAUserRequestForAValidUserId()
        {
            var endpoint = GetGroupsForUser(_c.UserApiConfig.TestConfig.ExistingUserId);
            _c.Request = RequestBuilder.Get(endpoint);
        }

        [Given(@"I have an add a user to a group request for a valid user id and valid group")]
        public void GivenIHaveAnAddAUserToAGroupRequestForAValidUserIdAndValidGroup()
        {
            var requestBody = new AddUserToGroupRequest()
            {
                UserId = _c.UserApiConfig.TestConfig.ExistingUserId,
                GroupName = _c.UserApiConfig.TestConfig.NewGroups.First().DisplayName
            };
            var endpoint = AddUserToGroup;
            _c.Request = RequestBuilder.Patch(endpoint, requestBody);
        }

        [Then(@"the ad groups should be retrieved")]
        public void ThenTheAdGroupsShouldBeRetrieved()
        {
            var model = RequestHelper.DeserialiseSnakeCaseJsonToResponse<GroupsResponse>(_c.Response.Content);
            model.Should().NotBeNull();
            model.DisplayName.Should().NotBeNullOrEmpty();
            model.GroupId.Should().NotBeNullOrEmpty();
        }

        [Then(@"a list of ad groups should be retrieved")]
        public void ThenAListOfAdGroupsShouldBeRetrieved()
        {
            var model = RequestHelper.DeserialiseSnakeCaseJsonToResponse<List<GroupsResponse>>(_c.Response.Content);
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
                userIsInTheGroup = await ActiveDirectoryUser.IsUserInAGroupAsync(_c.UserApiConfig.TestConfig.ExistingUserId,
                    _c.UserApiConfig.TestConfig.NewGroups.First().DisplayName, _c.GraphApiToken);
                await Task.Delay(DelayInMilliseconds);
            }

            sw.Stop();
            userIsInTheGroup.Should().BeTrue("User has been added to the group");
            _c.Test.NewGroupId = _c.UserApiConfig.TestConfig.NewGroups.First().GroupId;
        }
    }
}
