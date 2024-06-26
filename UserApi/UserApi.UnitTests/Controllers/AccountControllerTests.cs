﻿using System.Collections.Generic;
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

namespace UserApi.UnitTests.Controllers
{
    public class AccountControllerTests
    {
        private AccountController _controller;
        private Mock<IUserAccountService> _userAccountService;
        private AddUserToGroupRequest _request;


        [SetUp]
        public void Setup()
        {
            _userAccountService = new Mock<IUserAccountService>();
            var config = TelemetryConfiguration.CreateDefault();
            var client = new TelemetryClient(config);

            _request = Builder<AddUserToGroupRequest>.CreateNew()
               .With(x => x.GroupName = "TestGroup")
               .With(x => x.UserId = "johndoe")
               .Build();

            _userAccountService.Setup(u => u.GetGroupByNameAsync(_request.GroupName)).ReturnsAsync(new Group());
            _userAccountService.Setup(u => u.GetUserByFilterAsync(It.IsAny<string>())).ReturnsAsync(new User());

            _controller = new AccountController(_userAccountService.Object, client);
        }

        [Test]
        public async Task Should_add_user_to_group_for_given_request()
        {
            _userAccountService.Setup(x => x.GetGroupIdFromSettings(_request.GroupName)).Returns(System.Guid.NewGuid().ToString());
            var response = await _controller.AddUserToGroup(_request);
            response.Should().NotBeNull();
            var result = (AcceptedResult)response;
            result.StatusCode.Should().Be((int)HttpStatusCode.Accepted);
            _userAccountService.Verify(u => u.AddUserToGroupAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task Should_return_bad_request_with_invalid_AddUserToGroupRequest()
        {
            _request.GroupName = string.Empty;

            var result = await _controller.AddUserToGroup(_request);
            ((ObjectResult)result).Value.Should().BeOfType<ValidationProblemDetails>().Which.Errors[nameof(_request.GroupName)]
                .Should()
                .Contain("Require a GroupName");
        }

        [Test]
        public async Task Should_return_not_found_with_no_matching_group_by_name()
        {
            _userAccountService.Setup(u => u.GetGroupIdFromSettings(_request.GroupName)).Returns(string.Empty);

            var actionResult = (NotFoundObjectResult)await _controller.AddUserToGroup(_request);

            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        [Test]
        public async Task Should_return_not_found_with_no_matching_user_by_filter()
        {
            _userAccountService.Setup(u => u.GetGroupIdFromSettings(It.IsAny<string>()));

            var actionResult = (NotFoundObjectResult)await _controller.AddUserToGroup(_request);
            
            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        [Test]
        public async Task Should_get_group_by_name_from_api()
        {
            const string groupName = "VA";
            var response = new Group();
            var groupResponse = new GroupsResponse();

            _userAccountService.Setup(x => x.GetGroupByNameAsync(groupName)).ReturnsAsync(response);

            var actionResult = (OkObjectResult) await _controller.GetGroupByName(groupName);
            var actualResponse = (GroupsResponse) actionResult.Value;
            actualResponse!.DisplayName.Should().BeSameAs(groupResponse.DisplayName);
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
                DisplayName = "Ext"
            };
            var groupResponse = new GroupsResponse
            {
                DisplayName = "Ext",
                GroupId = "123"
            };

            _userAccountService.Setup(x => x.GetGroupByIdAsync(groupId)).ReturnsAsync(response);

            var actionResult = (OkObjectResult) await _controller.GetGroupById(groupId);
            var actualResponse = (GroupsResponse) actionResult.Value;
            actualResponse?.DisplayName.Should().BeSameAs(groupResponse.DisplayName);
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
                DisplayName = "Ext"
            };
            var response = new List<Group>
            {
                group
            };

            IEnumerable<GroupsResponse> groupResponseList = new[]
            {
                new GroupsResponse {DisplayName = "Ext"}
            };

            _userAccountService.Setup(x => x.GetGroupsForUserAsync(userId)).ReturnsAsync(response);

            var actionResult = (OkObjectResult) await _controller.GetGroupsForUser(userId);
            var actualResponse = (IEnumerable<GroupsResponse>) actionResult.Value;
            actualResponse!.FirstOrDefault()?.DisplayName.Should()
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