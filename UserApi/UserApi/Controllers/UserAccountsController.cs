using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UserApi.Contracts.Requests;
using UserApi.Contracts.Responses;
using UserApi.Services;
using UserApi.Validations;

namespace UserApi.Controllers
{
    [Produces("application/json")]
    [Route("api/accounts")]
    [ApiController]
    public class UserAccountsController : ControllerBase
    {
        private readonly IUserAccountService _userAccountService;
        private readonly TelemetryClient _telemetryClient;

        public UserAccountsController(IUserAccountService userAccountService, TelemetryClient telemetryClient)
        {
            _userAccountService = userAccountService;
            _telemetryClient = telemetryClient;
        }
        
        /// <summary>
        /// Create a new hearings reforms user account
        /// </summary>
        /// <param name="request">Details of a new user</param>
        [HttpPost("user", Name = "CreateUser")]
        [SwaggerOperation(OperationId = "CreateUser")]
        [ProducesResponseType(typeof(NewUserResponse), (int) HttpStatusCode.Created)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public IActionResult CreateUser(CreateUserRequest request)
        {
            var result = new CreateUserRequestValidation().Validate(request);
            
            if (!result.IsValid)
            {
                foreach (var failure in result.Errors)
                {
                    ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
                }

                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage)).ToList();
                _telemetryClient.TrackTrace(new TraceTelemetry(
                    $"CreateUserRequest validation failed: {string.Join("; ", errors)}",
                    SeverityLevel.Error));
                return BadRequest(ModelState);
            }

            var adUserAccount = _userAccountService.CreateUser(request.FirstName, request.LastName);
            _userAccountService.UpdateAuthenticationInformation(adUserAccount.UserId, request.RecoveryEmail);

            var response = new NewUserResponse
            {
                UserId = adUserAccount.UserId,
                Username = adUserAccount.Username,
                OneTimePassword = adUserAccount.OneTimePassword
            };
            return CreatedAtRoute("GetUserByAdUserId", new {userId = adUserAccount.UserId}, response);
        }
        
        /// <summary>
        /// Add a user to a group
        /// </summary>
        [HttpPatch("user/group", Name = "AddUserToGroup")]
        [SwaggerOperation(OperationId = "AddUserToGroup")]
        [ProducesResponseType((int) HttpStatusCode.Accepted)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public IActionResult AddUserToGroup(AddUserToGroupRequest request)
        {
            var result = new AddUserToGroupRequestValidation().Validate(request);
            
            if (!result.IsValid)
            {
                foreach (var failure in result.Errors)
                {
                    ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
                }

                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage)).ToList();
                _telemetryClient.TrackTrace(new TraceTelemetry(
                    $"AddUserToGroupRequest validation failed: {string.Join("; ", errors)}",
                    SeverityLevel.Error));
                return BadRequest(ModelState);
            }
            
            var group = _userAccountService.GetGroupByName(request.GroupName);
            if (group == null)
            {
                _telemetryClient.TrackTrace(new TraceTelemetry($"Group not found '{request.GroupName}'",
                    SeverityLevel.Error));
                return NotFound();
            }

            var user = _userAccountService.GetUserById(request.UserId);
            if (user == null)
            {
                _telemetryClient.TrackTrace(new TraceTelemetry($"User with ID '{request.UserId}' not found ",
                    SeverityLevel.Error));
                return NotFound();
            }
            
            _userAccountService.AddUserToGroup(user, group);
            return Accepted();
        }
        
        /// <summary>
        /// Assign a user's recovery email
        /// </summary>
        [HttpPatch("user/recovery", Name = "SetRecoveryInformation")]
        [SwaggerOperation(OperationId = "SetRecoveryInformation")]
        [ProducesResponseType((int) HttpStatusCode.Accepted)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public IActionResult SetRecoveryEmail()
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Get a user by recovery email
        /// </summary>
        [HttpGet("user", Name = "GetUserByRecoveryEmail")]
        [SwaggerOperation(OperationId = "GetUserByRecoveryEmail")]
        [ProducesResponseType((int) HttpStatusCode.Accepted)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public IActionResult GetUserByRecoveryEmail([FromQuery]string recoveryMail)
        {
            var user = _userAccountService.GetUserByAlternativeEmail(recoveryMail);
            if (user == null)
            {
                return NotFound();
            }

            var response = new UserDetailsResponse
            {
                UserId = user.Id,
                Username = user.UserPrincipalName,
                DisplayName = user.DisplayName
            };
            return Ok(response);
        }
        
        /// <summary>
        /// Get User by AD User ID
        /// </summary>
        [HttpGet("user/{userId}", Name = "GetUserByAdUserId")]
        [SwaggerOperation(OperationId = "GetUserByAdUserId")]
        [ProducesResponseType(typeof(UserDetailsResponse), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public IActionResult GetUserByAdUserId(string userId)
        {
            var user = _userAccountService.GetUserById(userId);
            if (user == null)
            {
                return NotFound();
            }

            var response = new UserDetailsResponse
            {
                UserId = user.Id,
                Username = user.UserPrincipalName,
                DisplayName = user.DisplayName
            };
            return Ok(response);
        }
        
        /// <summary>
        /// Get AD Group By Name
        /// </summary>
        [HttpGet("group", Name = "GetGroupByName")]
        [SwaggerOperation(OperationId = "GetGroupByName")]
        [ProducesResponseType(typeof(GroupsResponse),(int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public IActionResult GetGroupByName([FromQuery]string name)
        {
            var adGroup = _userAccountService.GetGroupByName(name);
            if (adGroup == null)
            {
                return NotFound();
            }

            var response = new GroupsResponse
            {
                GroupId = adGroup.Id,
                DisplayName = adGroup.DisplayName
            };
            return Ok(response);
        }
        
        /// <summary>
        /// Get AD Group By Id
        /// </summary>
        [HttpGet("group/{groupId}", Name = "GetGroupById")]
        [SwaggerOperation(OperationId = "GetGroupById")]
        [ProducesResponseType(typeof(GroupsResponse),(int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public IActionResult GetGroupById(string groupId)
        {
            var adGroup = _userAccountService.GetGroupById(groupId);
            if (adGroup == null)
            {
                return NotFound();
            }

            var response = new GroupsResponse
            {
                GroupId = adGroup.Id,
                DisplayName = adGroup.DisplayName
            };
            return Ok(response);
        }
        
        /// <summary>
        /// Get AD Group For a User
        /// </summary>
        [HttpGet("user/{userId}/groups", Name = "GetGroupsForUser")]
        [SwaggerOperation(OperationId = "GetGroupsForUser")]
        [ProducesResponseType(typeof(List<GroupsResponse>),(int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public IActionResult GetGroupsForUser(string userId)
        {
            var adGroups = _userAccountService.GetGroupsForUser(userId);
            if (adGroups == null || !adGroups.Any())
            {
                return NotFound();
            }

            var response = adGroups.Select(x => new GroupsResponse()
            {
                GroupId = x.Id,
                DisplayName = x.DisplayName
            });
            return Ok(response);
        }
    }
}