using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Moq;
using NUnit.Framework;
using UserApi.Contract.Responses;
using UserApi.Controllers;
using UserApi.Services;
using Testing.Common.Assertions;
using UserApi.Contract.Requests;
using FizzWare.NBuilder;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using UserApi.Security;

namespace UserApi.UnitTests.Controllers
{
    public class AccountControllerTests
    {
        private AccountController _controller;
        private Mock<IUserAccountService> _userAccountService;
        private AddUserToGroupRequest request;


        [SetUp]
        public void Setup()
        {
            _userAccountService = new Mock<IUserAccountService>();
            var config = TelemetryConfiguration.CreateDefault();
            var client = new TelemetryClient(config);

            request = Builder<AddUserToGroupRequest>.CreateNew()
               .With(x => x.GroupName = "TestGroup")
               .With(x => x.UserId = "johndoe")
               .Build();

            _userAccountService.Setup(u => u.GetGroupByNameAsync(request.GroupName)).ReturnsAsync(new Group());
            _userAccountService.Setup(u => u.GetUserByFilterAsync(It.IsAny<string>())).ReturnsAsync(new User());

            _controller = new AccountController(_userAccountService.Object, client);
        }

        [Test]
        public async Task Should_add_user_to_group_for_given_request()
        {
            var filter = $"objectId  eq '{request.UserId}'";

            var response = await _controller.AddUserToGroup(request);

            response.Should().NotBeNull();
            var result = (AcceptedResult)response;
            result.StatusCode.Should().Be((int)HttpStatusCode.Accepted);
            _userAccountService.Verify(u => u.AddUserToGroupAsync(It.IsAny<User>(), It.IsAny<Group>()), Times.Once);
            _userAccountService.Verify(u => u.GetUserByFilterAsync(filter), Times.Once);

        }

        [Test]
        public async Task Should_return_bad_request_with_invalid_AddUserToGroupRequest()
        {
            request.GroupName = string.Empty;

            var actionResult = (BadRequestObjectResult)await _controller.AddUserToGroup(request);
            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            ((SerializableError)actionResult.Value).ContainsKeyAndErrorMessage("GroupName", "Require a GroupName");
        }

        [Test]
        public async Task Should_return_not_found_with_no_matching_group_by_name()
        {
            _userAccountService.Setup(u => u.GetGroupByNameAsync(request.GroupName)).ReturnsAsync((Group)null);

            var actionResult = (NotFoundResult)await _controller.AddUserToGroup(request);

            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        [Test]
        public async Task Should_return_not_found_with_no_matching_user_by_filter()
        {
            _userAccountService.Setup(u => u.GetUserByFilterAsync(It.IsAny<string>())).ReturnsAsync((User)null);

            var actionResult = (NotFoundObjectResult)await _controller.AddUserToGroup(request);
            
            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            ((ModelStateDictionary)actionResult.Value).ContainsKeyAndErrorMessage("user", "User not found");
        }

        [Test]
        public async Task Should_return_not_found_with_UserServiceException()
        {
            _userAccountService.Setup(u => u.AddUserToGroupAsync(It.IsAny<User>(), It.IsAny<Group>())).ThrowsAsync(new UserServiceException("",""));

            var actionResult = (NotFoundObjectResult)await _controller.AddUserToGroup(request);

            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            ((ModelStateDictionary)actionResult.Value).ContainsKeyAndErrorMessage("user", "user already exists");
        }

        [Test]
        public async Task Should_get_group_by_name_from_api()
        {
            const string groupName = "VirtualRoomAdministrator";
            var response = new Group();
            var groupResponse = new GroupsResponse();

            _userAccountService.Setup(x => x.GetGroupByNameAsync(groupName)).ReturnsAsync(response);

            var actionResult = (OkObjectResult) await _controller.GetGroupByName(groupName);
            var actualResponse = (GroupsResponse) actionResult.Value;
            actualResponse.DisplayName.Should().BeSameAs(groupResponse.DisplayName);
            actualResponse.GroupId.Should().BeSameAs(groupResponse.GroupId);
        }

        [Test]
        public async Task Should_return_bad_request_with_invalid_groupName()
        {
            var name = string.Empty;

            var actionResult = (BadRequestObjectResult)await _controller.GetGroupByName(name);
            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            ((SerializableError)actionResult.Value).ContainsKeyAndErrorMessage(nameof(name), $"Please provide a valid {nameof(name)}");
        }

        [Test]
        public async Task Should_get_group_by_id_from_api()
        {
            const string groupId = "123";
            var response = new Group
            {
                DisplayName = "External"
            };
            var groupResponse = new GroupsResponse
            {
                DisplayName = "External",
                GroupId = "123"
            };

            _userAccountService.Setup(x => x.GetGroupByIdAsync(groupId)).ReturnsAsync(response);

            var actionResult = (OkObjectResult) await _controller.GetGroupById(groupId);
            var actualResponse = (GroupsResponse) actionResult.Value;
            actualResponse.DisplayName.Should().BeSameAs(groupResponse.DisplayName);
        }

        [Test]
        public async Task Should_return_bad_request_with_invalid_groupId()
        {
            var groupId = string.Empty;

            var actionResult = (BadRequestObjectResult)await _controller.GetGroupById(groupId);
            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            ((SerializableError)actionResult.Value).ContainsKeyAndErrorMessage(nameof(groupId), $"Please provide a valid {nameof(groupId)}");
        }

        [Test]
        public async Task Should_get_groups_for_user_by_user_id_from_api()
        {
            const string userId = "123";
            var group = new Group
            {
                DisplayName = "External"
            };
            var response = new List<Group>
            {
                group
            };

            IEnumerable<GroupsResponse> groupResponseList = new[]
            {
                new GroupsResponse {DisplayName = "External"}
            };

            _userAccountService.Setup(x => x.GetGroupsForUserAsync(userId)).ReturnsAsync(response);

            var actionResult = (OkObjectResult) await _controller.GetGroupsForUser(userId);
            var actualResponse = (IEnumerable<GroupsResponse>) actionResult.Value;
            actualResponse.FirstOrDefault()?.DisplayName.Should()
                .BeSameAs(groupResponseList.FirstOrDefault()?.DisplayName);
        }

        [Test]
        public async Task Should_return_bad_request_with_invalid_userid()
        {
            var userId = string.Empty;

            var actionResult = (BadRequestObjectResult)await _controller.GetGroupsForUser(userId);
            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            ((SerializableError)actionResult.Value).ContainsKeyAndErrorMessage(nameof(userId), $"Please provide a valid {nameof(userId)}");
        }

        [Test]
        public async Task Should_return_not_found_when_no_matching_group_is_found()
        {
            const string userId = "123";             

            _userAccountService.Setup(x => x.GetGroupsForUserAsync(userId)).ReturnsAsync((List<Group>)null);

            var actionResult = (NotFoundResult)await _controller.GetGroupsForUser(userId);
            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }
    }
}