using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using FluentAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Graph;
using Moq;
using NUnit.Framework;
using Testing.Common.Assertions;
using UserApi.Contract.Requests;
using UserApi.Contract.Responses;
using UserApi.Controllers;
using UserApi.Services;
using UserApi.Services.Models;
namespace UserApi.UnitTests.Controllers
{
    public class UserAccountsControllerTests
    {
        private UserController _controller;
        private Mock<IUserAccountService> _userAccountService;
        private CreateUserRequest _request;
        private NewAdUserAccount _newAdUserAccount;
        private Settings _settings;
        protected const string Domain = "@hearings.test.server.net";

        [SetUp]
        public void Setup()
        {
            _userAccountService = new Mock<IUserAccountService>();
            var representativeGroups = new List<Group> {new Group {DisplayName = "ProfUser"}};
            _userAccountService.Setup(x => x.GetGroupsForUserAsync(It.IsAny<string>()))
                .ReturnsAsync(representativeGroups);
            var config = TelemetryConfiguration.CreateDefault();
            var client = new TelemetryClient(config);
            _settings = new Settings { IsLive = true,
                                        ReformEmail = Domain.Replace("@", ""), AdGroup = new AdGroup(),
                                    };

            _request = Builder<CreateUserRequest>.CreateNew()
                .With(x => x.FirstName = "John")
                .With(x => x.LastName = "doe")
                .With(x => x.RecoveryEmail = "john.doe@hmcts.net")
                .Build();
            _newAdUserAccount = new NewAdUserAccount { UserId = "TestUserId", Username = "TestUserName", OneTimePassword = "TestPassword" };
            _userAccountService.Setup(u => u.CreateUserAsync(_request.FirstName, _request.LastName, _request.RecoveryEmail, _request.IsTestUser)).ReturnsAsync(_newAdUserAccount);
            
            _controller = new UserController(_userAccountService.Object, client, _settings);
        }

        [Test]
        public async Task Should_create_user_and_return_NewUserResponse_for_given_request()
        {
            var actionResult = (CreatedAtRouteResult)await _controller.CreateUser(_request);

            actionResult.Should().NotBeNull();
            actionResult.RouteName.Should().Be("GetUserByAdUserId");
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.Created);
            var response = (NewUserResponse)actionResult.Value;
            response.UserId.Should().Be(_newAdUserAccount.UserId);
            response.Username.Should().Be(_newAdUserAccount.Username);
            response.OneTimePassword.Should().Be(_newAdUserAccount.OneTimePassword);
        }

        [Test]
        public async Task Should_return_BadRequest_for_given_invalid_create_user_request()
        {
            var actionResult = (BadRequestObjectResult)await _controller.CreateUser(new CreateUserRequest() { FirstName = "Test", LastName = "Tester" });

            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            var serializableErrors = (SerializableError)actionResult.Value;
            serializableErrors.ContainsKeyAndErrorMessage("RecoveryEmail","recovery email cannot be empty");
        }

        [Test]
        public async Task Should_return_ConflictObjectResult_with_UserExistsException()
        {
            _userAccountService.Setup(u => u.CreateUserAsync(_request.FirstName, _request.LastName, _request.RecoveryEmail, _request.IsTestUser)).ThrowsAsync(new UserExistsException("User exists","TestUser"));

            var actionResult = (ConflictObjectResult)await _controller.CreateUser(_request);

            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
            var actualResponse = (NewUserErrorResponse) actionResult.Value;
            actualResponse.Message.Should().Be("User already exists");
            actualResponse.Code.Should().Be("UserExists");
            actualResponse.Username.Should().Be("TestUser");
        }

        [Test]
        public async Task Should_return_ConflictObjectResult_with_InvalidEmailException()
        {
            _userAccountService.Setup(u => u.CreateUserAsync(_request.FirstName, _request.LastName, _request.RecoveryEmail, _request.IsTestUser)).ThrowsAsync(new InvalidEmailException("Recovery email is not a valid email", _request.RecoveryEmail));
            var actionResult = (ConflictObjectResult)await _controller.CreateUser(_request);

            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
            var actualResponse = (NewUserErrorResponse) actionResult.Value;
            actualResponse.Message.Should().Be("Recovery email is not a valid email");
            actualResponse.Code.Should().Be("InvalidEmail");
            actualResponse.Email.Should().Be(_request.RecoveryEmail);
        }

        [Test]
        public async Task Should_get_user_by_user_id_from_api()
        {
            string userId = Guid.NewGuid().ToString();
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

            var filter = $"id  eq '{userId}'";
            _userAccountService.Setup(x => x.GetUserByFilterAsync(filter)).Returns(Task.FromResult(userResponse));

            var actionResult = (OkObjectResult) await _controller.GetUserByAdUserId(userId);
            var actualResponse = (UserProfile) actionResult.Value;
            actualResponse.DisplayName.Should().BeSameAs(response.DisplayName);
            actualResponse.FirstName.Should().BeSameAs(response.FirstName);
            actualResponse.LastName.Should().BeSameAs(response.LastName);
        }
        
        [Test]
        public async Task Should_get_user_by_user_id_from_api_with_special_characters()
        {
            string userId = "john.o'conner@hearings.reform.hmcts.net ";
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

            var filter = $"id  eq '{userId.Replace("'", "''")}'";
            _userAccountService.Setup(x => x.GetUserByFilterAsync(filter)).Returns(Task.FromResult(userResponse));

            var actionResult = (OkObjectResult) await _controller.GetUserByAdUserId(userId);
            var actualResponse = (UserProfile) actionResult.Value;
            actualResponse.DisplayName.Should().BeSameAs(response.DisplayName);
            actualResponse.FirstName.Should().BeSameAs(response.FirstName);
            actualResponse.LastName.Should().BeSameAs(response.LastName);
        }

        [Test]
        public async Task Should_return_notfound_with_no_matching_user_profile()
        {
            var userId = Guid.NewGuid().ToString();
            _userAccountService.Setup(x => x.GetUserByFilterAsync(It.IsAny<string>())).Returns(Task.FromResult((User)null));

            var actionResult = (NotFoundObjectResult)await _controller.GetUserByAdUserId(userId);

            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            ((ModelStateDictionary)actionResult.Value).ContainsKeyAndErrorMessage(nameof(userId), "user does not exist");
        }

        [Test]
        public async Task Should_get_user_by_user_name_from_api()
        {
            const string userName = "sample.user'test@hearings.test.server.net";
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

            var filter = $"userPrincipalName  eq '{userName.Replace("'", "''")}'";
            _userAccountService.Setup(x => x.GetUserByFilterAsync(filter)).Returns(Task.FromResult(userResponse));

            var actionResult = (OkObjectResult) await _controller.GetUserByUserName(userName);
            var actualResponse = (UserProfile) actionResult.Value;
            actualResponse.DisplayName.Should().BeSameAs(response.DisplayName);
            actualResponse.FirstName.Should().BeSameAs(response.FirstName);
            actualResponse.LastName.Should().BeSameAs(response.LastName);
            _userAccountService.Verify(x => x.GetUserByFilterAsync(filter),Times.Once);
        }

        [Test]
        public async Task Should_get_unauthorized_when_get_by_user_name_from_api()
        {
            const string userName = "sample.user@hearings.test.server.net";
            _userAccountService
                .Setup(x => x.GetUserByFilterAsync(It.IsAny<string>()))
                .Throws(new UnauthorizedAccessException("unauthorized"));

            var result = (await _controller.GetUserByUserName(userName)) as UnauthorizedObjectResult;
            Assert.NotNull(result);
        }

        [Test]
        public async Task Should_get_notfound_with_no_matching_user_profile()
        {
            const string userName = "sample.user@hearings.test.server.net";
            _userAccountService.Setup(x => x.GetUserByFilterAsync(It.IsAny<string>())).Returns(Task.FromResult((User)null));

            var actionResult = (NotFoundObjectResult)await _controller.GetUserByUserName(userName);

            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            ((ModelStateDictionary)actionResult.Value).ContainsKeyAndErrorMessage(nameof(userName), "user principal name does not exist");
        }

        [Test]
        public async Task Should_return_badrequest_with_invalid_userName()
        {
            var userName = string.Empty;

            var actionResult = (BadRequestObjectResult)await _controller.GetUserByUserName(userName);
            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            ((SerializableError)actionResult.Value).ContainsKeyAndErrorMessage(nameof(userName), "user principal name cannot be empty");
        }

        [Test]
        public async Task Should_get_user_by_email_from_api()
        {
            const string email = "sample.user'test@hmcts.net";
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

            var filter = $"otherMails/any(c:c eq '{email.Replace("'", "''")}')";
            _userAccountService.Setup(x => x.GetUserByFilterAsync(filter)).Returns(Task.FromResult(userResponse));

            var actionResult = (OkObjectResult) await _controller.GetUserByEmail(email);
            var actualResponse = (UserProfile) actionResult.Value;
            actualResponse.DisplayName.Should().BeSameAs(response.DisplayName);
            actualResponse.FirstName.Should().BeSameAs(response.FirstName);
            actualResponse.LastName.Should().BeSameAs(response.LastName);
            _userAccountService.Verify(x => x.GetUserByFilterAsync(filter), Times.Once);
        }

        [Test]
        public async Task Should_return_badrequest_with_no_emailid()
        {
            var email = string.Empty;

            var actionResult = (BadRequestObjectResult)await _controller.GetUserByEmail(email);
            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            ((SerializableError)actionResult.Value).ContainsKeyAndErrorMessage(nameof(email), "email cannot be empty");
        }

        [Test]
        public async Task Should_return_badrequest_with_invalid_email()
        {
            const string firstName = "Automatically";
            const string lastName = "Created";
            var unique = DateTime.Now.ToString("yyyyMMddhmmss");
            var email = $"{firstName}.{lastName}.{unique}.@hearings.reform.hmcts.net"; // dot before @ is invalid email formatting

            var actionResult = (NotFoundObjectResult)await _controller.GetUserByEmail(email);
            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            ((ModelStateDictionary)actionResult.Value).ContainsKeyAndErrorMessage("email", "email does not exist");
        }

        [Test]
        public async Task Should_get_users_for_group_by_group_id_from_api()
        {
            var userList = new List<UserResponse>()
            {
                new UserResponse { DisplayName = "firstname lastname", FirstName = "firstname", LastName = "lastname", Email = "firstname.lastname@hearings.test.server.net" },
                new UserResponse { DisplayName = "firstname1 lastname1", FirstName = "firstname1", LastName = "lastname1", Email = "firstname1.lastname1@hearings.test.server.net"}
            };
            _userAccountService.Setup(x => x.GetJudgesAsync(It.IsAny<string>())).ReturnsAsync(userList);

            var actionResult = (OkObjectResult)await _controller.GetJudgesByUsername("firstname");
            var actualResponse = (List<UserResponse>)actionResult.Value;
            actualResponse.Count.Should().Be(2);
            actualResponse.FirstOrDefault().DisplayName.Should().BeSameAs(userList.FirstOrDefault().DisplayName);
        }

        [Test]
        public async Task Should_get_empty_user_response_without_judges()
        {
            var actionResult = (OkObjectResult)await _controller.GetJudgesByUsername("firstname.lastname@hearings.test.server.net");
            var actualResponse = (List<UserResponse>)actionResult.Value;
            actualResponse.Count.Should().Be(0);
        }


        [Test]
        public async Task GetJudgesByUsername_Should_get_users_for_group_by_group_id_from_api()
        {
            var response = new List<UserResponse>();
            var user = new UserResponse
            {
                DisplayName = "firstname lastname",
                FirstName = "firstname",
                LastName = "lastname",
                Email = "firstname.lastname@hearings.test.server.net"
            };
            response.Add(user);

            var user2 = new UserResponse
            {
                DisplayName = "firstname1 lastname1",
                FirstName = "firstname1",
                LastName = "lastname1",
                Email = "firstname1.lastname1@hearings.test.server.net"
            };
            response.Add(user2);

            var term = "firstname";

            _userAccountService.Setup(x => x.GetJudgesAsync(It.IsAny<string>()))
                .ReturnsAsync(response.AsEnumerable());

            var actionResult = (OkObjectResult)await _controller.GetJudgesByUsername(term);
            var actualResponse = (List<UserResponse>)actionResult.Value;
            actualResponse.Count.Should().Be(2);
            actualResponse[0].DisplayName.Should().BeSameAs(user.DisplayName);
            actualResponse[1].DisplayName.Should().BeSameAs(user2.DisplayName);
        }

        [Test]
        public async Task GetJudgesByUsername_Should_get_empty_user_response_without_judges()
        {
            _userAccountService.Setup(x => x.GetJudgesAsync(It.IsAny<string>()))
                .ReturnsAsync((IEnumerable<UserResponse>)null);

            var actionResult = (OkObjectResult)await _controller.GetJudgesByUsername("username");
            var actualResponse = (List<UserResponse>)actionResult.Value;
            actualResponse.Count.Should().Be(0);
        }

        [Test]
        public async Task Should_get_ejudiciary_judges_for_group_by_group_id_from_api()
        {
            var response = new List<UserResponse>();
            var user = new UserResponse
            {
                DisplayName = "firstname lastname",
                FirstName = "firstname",
                LastName = "lastname",
                Email = "firstname.lastname@hearings.test.server.net"
            };

            response.Add(user);

            var user2 = new UserResponse
            {
                DisplayName = "firstname1 lastname1",
                FirstName = "firstname1",
                LastName = "lastname1",
                Email = "firstname1.lastname1@hearings.test.server.net"
            };
            response.Add(user2);

            var term = "firstname";
            _userAccountService.Setup(x => x.GetEjudiciaryJudgesAsync(term)).ReturnsAsync(response.Where(x => x.Email.Contains(term)).ToList());

            var actionResult = (OkObjectResult)await _controller.GetEjudiciaryJudgesByUsername(term);
            var actualResponse = (List<UserResponse>)actionResult.Value;
            actualResponse.Count.Should().Be(2);
            actualResponse[0].DisplayName.Should().BeSameAs(user.DisplayName);
            actualResponse[1].DisplayName.Should().BeSameAs(user2.DisplayName);

            term = "firstname1";
            _userAccountService.Setup(x => x.GetEjudiciaryJudgesAsync(term)).ReturnsAsync(response.Where(x => x.Email.Contains(term)).ToList());

            actionResult = (OkObjectResult)await _controller.GetEjudiciaryJudgesByUsername(term);
            actualResponse = (List<UserResponse>)actionResult.Value;
            actualResponse.Count.Should().Be(1);
            actualResponse[0].DisplayName.Should().BeSameAs(user2.DisplayName);
        }

        [Test]
        public async Task Should_get_empty_ejudiciary_judges_response_without_judges()
        {
            var actionResult = (OkObjectResult)await _controller.GetEjudiciaryJudgesByUsername("firstname1");
            var actualResponse = (List<UserResponse>)actionResult.Value;
            actualResponse.Count.Should().Be(0);
        }

        [Test]
        public async Task Should_return_bad_request_for_update_user()
        {
            var username = " ";            

            (await _controller.ResetUserPassword(null)).Should().NotBeNull().And.BeAssignableTo<BadRequestObjectResult>();
            (await _controller.ResetUserPassword(string.Empty)).Should().NotBeNull().And.BeAssignableTo<BadRequestObjectResult>();
            var actionResult = (BadRequestObjectResult)await _controller.ResetUserPassword(username);

            actionResult.Should().NotBeNull();
            actionResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            ((SerializableError)actionResult.Value).ContainsKeyAndErrorMessage(nameof(username), "username cannot be empty");
        }
        [Test]
        public async Task Should_update_the_password_for_a_user_that_exists_in_ad()
        {
            const string email = "known.user'test@hmcts.net";
            var filter = $"userPrincipalName  eq '{email.Replace("'", "''")}'";
            var userResponse = new User
            {
                DisplayName = "Sample User",
                GivenName = "User",
                Surname = "Sample",
                UserPrincipalName = email
            };

            const string password = "Password123";
            _userAccountService.Setup(x => x.GetUserByFilterAsync(filter)).ReturnsAsync(userResponse);
            _userAccountService.Setup(x => x.UpdateUserPasswordAsync(userResponse.UserPrincipalName)).ReturnsAsync(password);

            var result = await _controller.ResetUserPassword(email);
            
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<OkObjectResult>();
            var response = (OkObjectResult) result;
            response.Should().NotBeNull();
            response.Value.Should().NotBeNull().And.BeAssignableTo<UpdateUserResponse>();
            response.Value.As<UpdateUserResponse>().NewPassword.Should().Be(password);
            
            _userAccountService.Verify(x => x.GetUserByFilterAsync(filter), Times.Once);
        }
    }
}
