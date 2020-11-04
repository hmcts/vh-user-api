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
using UserApi.Security;
using UserApi.Services;
using UserApi.Validations;

namespace UserApi.Controllers
{
    [Produces("application/json")]
    [Route("accounts")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IUserAccountService _userAccountService;
        private const string separator = "; ";

        public AccountController(IUserAccountService userAccountService, TelemetryClient telemetryClient)
        {
            _userAccountService = userAccountService;
            _telemetryClient = telemetryClient;
        }

        /// <summary>
        ///     Get AD Group By Name
        /// </summary>
        [HttpGet("group", Name = "GetGroupByName")]
        [SwaggerOperation(OperationId = "GetGroupByName")]
        [ProducesResponseType(typeof(GroupsResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetGroupByName([FromQuery] string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                ModelState.AddModelError(nameof(name), $"Please provide a valid {nameof(name)}");
                return BadRequest(ModelState);
            }

            var adGroup = await _userAccountService.GetGroupByNameAsync(name);
            if (adGroup == null) return NotFound();

            var response = new GroupsResponse
            {
                GroupId = adGroup.Id,
                DisplayName = adGroup.DisplayName
            };

            return Ok(response);
        }

        /// <summary>
        ///     Get AD Group By Id
        /// </summary>
        [HttpGet("group/{groupId?}", Name = "GetGroupById")]
        [SwaggerOperation(OperationId = "GetGroupById")]
        [ProducesResponseType(typeof(GroupsResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetGroupById(string groupId)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                {
                    ModelState.AddModelError(nameof(groupId), $"Please provide a valid {nameof(groupId)}");
                    return BadRequest(ModelState);
                }

                var adGroup = await _userAccountService.GetGroupByIdAsync(groupId);
                if (adGroup == null) return NotFound();

                var response = new GroupsResponse
                {
                    GroupId = adGroup.Id,
                    DisplayName = adGroup.DisplayName
                };
                return Ok(response);
            }
            catch (UserServiceException)
            {
                return NotFound();
            }
        }

        /// <summary>
        ///     Get AD Group For a User
        /// </summary>
        [HttpGet("user/{userId?}/groups", Name = "GetGroupsForUser")]
        [SwaggerOperation(OperationId = "GetGroupsForUser")]
        [ProducesResponseType(typeof(List<GroupsResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetGroupsForUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError(nameof(userId), $"Please provide a valid {nameof(userId)}");
                return BadRequest(ModelState);
            }

            var adGroups = await _userAccountService.GetGroupsForUserAsync(userId);
            if (adGroups == null || !adGroups.Any()) return NotFound();

            var response = adGroups.Select(x => new GroupsResponse
            {
                GroupId = x.Id,
                DisplayName = x.DisplayName
            });
            return Ok(response);
        }

        /// <summary>
        ///     Add a user to a group
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
                    ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);

                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage)).ToList();
                _telemetryClient.TrackTrace(new TraceTelemetry(
                    $"AddUserToGroupRequest validation failed: {string.Join(separator, errors)}",
                    SeverityLevel.Error));
                return BadRequest(ModelState);
            }

            var group = await _userAccountService.GetGroupByNameAsync(request.GroupName);
            if (group == null)
            {
                _telemetryClient.TrackTrace(new TraceTelemetry($"Group not found '{request.GroupName}'",
                    SeverityLevel.Error));
                return NotFound();
            }

            var filter = $"objectId  eq '{request.UserId}'";
            var user = await _userAccountService.GetUserByFilterAsync(filter);
            if (user == null)
            {
                _telemetryClient.TrackTrace($"User with ID '{request.UserId}' not found ", SeverityLevel.Error);

                ModelState.AddModelError(nameof(user), "User not found");
                return NotFound(ModelState);
            }

            try
            {
                await _userAccountService.AddUserToGroupAsync(user, group);
            }
            catch (UserServiceException ex)
            {
                ModelState.AddModelError(nameof(user), "user already exists");
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    {"problem", $"{ex.GetType().Name} - {ex.Message} : {ex.InnerException?.Message}"},
                    {"request.UserId", request.UserId},
                    {"request.GroupName", request.GroupName}
                });
                
                return NotFound(ModelState);
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    {"problem", $"{ex.GetType().Name} - {ex.Message} : {ex.InnerException?.Message}"},
                    {"request.UserId", request.UserId},
                    {"request.GroupName", request.GroupName}
                });

                throw;
            }

            return Accepted();
        }
    }
}