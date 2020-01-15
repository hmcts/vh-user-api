using System.Collections.Generic;
using System.Linq;
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

namespace UserApi.UnitTests.Controllers
{
    public class AccountControllerTests
    {
        private AccountController _controller;
        private Mock<IUserAccountService> _userAccountService;

        [SetUp]
        public void Setup()
        {
            _userAccountService = new Mock<IUserAccountService>();
            var config = TelemetryConfiguration.CreateDefault();
            var client = new TelemetryClient(config);
            _controller = new AccountController(_userAccountService.Object, client);
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
    }
}