using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UserApi.Contract.Requests;
using UserApi.Contract.Responses;
using UserApi.Helper;
using UserApi.Services;
using UserApi.Services.Models;
using UserApi.Validations;

namespace UserApi.Controllers
{
    [Produces("application/json")]
    [Route("accounts")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IUserAccountService _userAccountService;
        private readonly TelemetryClient _telemetryClient;

        public AccountController(IUserAccountService userAccountService, TelemetryClient telemetryClient)
        {
            _userAccountService = userAccountService;
            _telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Get AD Group By Name
        /// </summary>
        [HttpGet("group", Name = "GetGroupByName")]
        [SwaggerOperation(OperationId = "GetGroupByName")]
        [ProducesResponseType(typeof(GroupsResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetGroupByName([FromQuery]string name)
        {
            var adGroup = await _userAccountService.GetGroupByName(name);
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
        [ProducesResponseType(typeof(GroupsResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetGroupById(string groupId)
        {
            var adGroup = await _userAccountService.GetGroupById(groupId);
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
        [ProducesResponseType(typeof(List<GroupsResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetGroupsForUser(string userId)
        {
            var adGroups = await _userAccountService.GetGroupsForUser(userId);
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

        /// <summary>
        /// Add a user to a group
        /// </summary>
        [HttpPatch("user/group", Name = "AddUserToGroup")]
        [SwaggerOperation(OperationId = "AddUserToGroup")]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> AddUserToGroup(AddUserToGroupRequest request)
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

            var group = await _userAccountService.GetGroupByName(request.GroupName);
            if (group == null)
            {
                _telemetryClient.TrackTrace(new TraceTelemetry($"Group not found '{request.GroupName}'",
                    SeverityLevel.Error));
                return NotFound();
            }

            var user = await _userAccountService.GetUserById(request.UserId);
            if (user == null)
            {
                _telemetryClient.TrackTrace(new TraceTelemetry($"User with ID '{request.UserId}' not found ",
                    SeverityLevel.Error));
                return NotFound();
            }

            await _userAccountService.AddUserToGroup(user, group);
            return Accepted();
        }
    }
}