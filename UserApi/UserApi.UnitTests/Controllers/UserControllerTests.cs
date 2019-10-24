using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Moq;
using NUnit.Framework;
using UserApi.Controllers;
using UserApi.Helper;
using UserApi.Services;
using UserApi.Services.Models;
namespace UserApi.UnitTests.Controllers
{
    public class UserAccountsControllerTests
    {
        private UserController _controller;
        private Mock<IUserAccountService> _userAccountService;

        [SetUp]
        public void Setup()
        {
            _userAccountService = new Mock<IUserAccountService>();
            var representativeGroups = new List<Group> {new Group { DisplayName = "VirtualRoomProfessionalUser" } };
            _userAccountService.Setup(x => x.GetGroupsForUserAsync(It.IsAny<string>())).ReturnsAsync(representativeGroups);
            _controller = new UserController(_userAccountService.Object, new TelemetryClient());
        }

        [Test]
        public async Task Should_get_user_by_user_id_from_api()
        {
            const string userId = "b67d648b-f226-4880-88e1-51b6d1ec7da7";
            var userResponse = new User
            {
                DisplayName = "Sample User",
                GivenName = "User",
                Surname = "Sample"
            };
            var response = new UserProfile
            {
                DisplayName = "Sample User",
                FirstName = "User",
                LastName = "Sample"
            };

            var filter = $"objectId  eq '{userId}'";
            _userAccountService.Setup(x => x.GetUserByFilterAsync(filter)).Returns(Task.FromResult(userResponse));

            var actionResult = (OkObjectResult) await _controller.GetUserByAdUserId(userId);
            var actualResponse = (UserProfile) actionResult.Value;
            actualResponse.DisplayName.Should().BeSameAs(response.DisplayName);
            actualResponse.FirstName.Should().BeSameAs(response.FirstName);
            actualResponse.LastName.Should().BeSameAs(response.LastName);
        }


        [Test]
        public async Task Should_get_user_by_user_name_from_api()
        {
            const string userName = "sample.user@hearings.reform.hmcts.net";
            var userResponse = new User
            {
                DisplayName = "Sample User",
                GivenName = "User",
                Surname = "Sample"
            };
            var response = new UserProfile
            {
                DisplayName = "Sample User",
                FirstName = "User",
                LastName = "Sample"
            };

            var filter = $"userPrincipalName  eq '{userName}'";
            _userAccountService.Setup(x => x.GetUserByFilterAsync(filter)).Returns(Task.FromResult(userResponse));

            var actionResult = (OkObjectResult) await _controller.GetUserByUserName(userName);
            var actualResponse = (UserProfile) actionResult.Value;
            actualResponse.DisplayName.Should().BeSameAs(response.DisplayName);
            actualResponse.FirstName.Should().BeSameAs(response.FirstName);
            actualResponse.LastName.Should().BeSameAs(response.LastName);
        }

        [Test]
        public async Task Should_get_unauthorized_when_get_by_user_name_from_api()
        {
            const string userName = "sample.user@hearings.reform.hmcts.net";
            _userAccountService
                .Setup(x => x.GetUserByFilterAsync(It.IsAny<string>()))
                .Throws(new UnauthorizedAccessException("unauthorized"));

            var result = (await _controller.GetUserByUserName(userName)) as UnauthorizedObjectResult;
            Assert.NotNull(result);
        }

        [Test]
        public async Task Should_get_user_by_email_from_api()
        {
            const string email = "sample.user@gmail.com";
            var userResponse = new User
            {
                DisplayName = "Sample User",
                GivenName = "User",
                Surname = "Sample"
            };
            var response = new UserProfile
            {
                DisplayName = "Sample User",
                FirstName = "User",
                LastName = "Sample"
            };

            var filter = $"otherMails/any(c:c eq '{email}')";
            _userAccountService.Setup(x => x.GetUserByFilterAsync(filter)).Returns(Task.FromResult(userResponse));

            var actionResult = (OkObjectResult) await _controller.GetUserByEmail(email);
            var actualResponse = (UserProfile) actionResult.Value;
            actualResponse.DisplayName.Should().BeSameAs(response.DisplayName);
            actualResponse.FirstName.Should().BeSameAs(response.FirstName);
            actualResponse.LastName.Should().BeSameAs(response.LastName);
        }

        [Test]
        public async Task Should_get_users_for_group_by_group_id_from_api()
        {
            var response = new List<UserResponse>();
            var user = new UserResponse() { DisplayName = "firstname lastname", FirstName = "firstname", LastName = "lastname", Email = "firstname.lastname@hearings.reform.hmcts.net" };
            response.Add(user);
            user = new UserResponse() { DisplayName = "firstname1 lastname1", FirstName = "firstname1", LastName = "lastname1", Email = "firstname1.lastname1@hearings.reform.hmcts.net" };
            response.Add(user);

            List<UserResponse> userList = new List<UserResponse>()
            {
                new UserResponse() { DisplayName = "firstname lastname", FirstName = "firstname", LastName = "lastname", Email = "firstname.lastname@hearings.reform.hmcts.net" }
            };

            _userAccountService.Setup(x => x.GetJudgesAsync()).Returns(Task.FromResult(response));
            var actionResult = (OkObjectResult)await _controller.GetJudges();
            var actualResponse = (List<UserResponse>)actionResult.Value;
            actualResponse.Count.Should().BeGreaterThan(0);
            actualResponse.FirstOrDefault().DisplayName.Should()
                .BeSameAs(userList.FirstOrDefault().DisplayName);
        }
    }
}