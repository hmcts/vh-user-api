using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UserApi.Caching;
using UserApi.Contract.Requests;
using UserApi.Contract.Responses;
using UserApi.Helper;
using UserApi.Mappers;
using UserApi.Services;
using UserApi.Validations;

namespace UserApi.Controllers
{
    [Produces("application/json")]
    [Route("users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ICache _distributedCache;
        private readonly IUserAccountService _userAccountService;
        private const string Separator = "; ";

        public UserController(IUserAccountService userAccountService, TelemetryClient telemetryClient, ICache distributedCache, Settings settings)
        {
            _userAccountService = userAccountService;
            _telemetryClient = telemetryClient;
            _distributedCache = distributedCache;
        }

        /// <summary>
        ///     Create a new hearings reforms user account
        /// </summary>
        /// <param name="request">Details of a new user</param>
        [HttpPost(Name = "CreateUser")]
        [OpenApiOperation("CreateUser")]
        [ProducesResponseType(typeof(NewUserResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CreateUser(CreateUserRequest request)
        {
            var result = new CreateUserRequestValidation().Validate(request);

            if (!result.IsValid)
            {
                foreach (var failure in result.Errors)
                    ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);

                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage)).ToList();
                _telemetryClient.TrackTrace(new TraceTelemetry(
                    $"CreateUserRequest validation failed: {string.Join(Separator, errors)}",
                    SeverityLevel.Error));
                return BadRequest(ModelState);
            }

            try
            {
                var adUserAccount =
                    await _userAccountService.CreateUserAsync(request.FirstName, request.LastName,
                        request.RecoveryEmail, request.IsTestUser);

                var response = new NewUserResponse
                {
                    UserId = adUserAccount.UserId,
                    Username = adUserAccount.Username,
                    OneTimePassword = adUserAccount.OneTimePassword
                };
                return CreatedAtRoute("GetUserByAdUserId", new {userId = adUserAccount.UserId}, response);
            }
            catch (UserExistsException e)
            {
                return new ConflictObjectResult(new
                {
                    Message = "User already exists",
                    Code = "UserExists",
                    e.Username
                });
            }
            catch (InvalidEmailException e) 
            {
                return new ConflictObjectResult(new
                {
                    Message = e.Message,
                    Code = "InvalidEmail",
                    e.Email
                });
            }
        }

        /// <summary>
        ///     Get User by AD User ID
        /// </summary>
        [HttpGet("{userId}", Name = "GetUserByAdUserId")]
        [OpenApiOperation("GetUserByAdUserId")]
        [ProducesResponseType(typeof(UserProfile), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserByAdUserId(string userId)
        {
            var filterText = userId.Replace("'", "''");
            var filter = $"objectId  eq '{filterText}'";
            var profile = new UserProfileHelper(_userAccountService);
            var userProfile = await profile.GetUserProfileAsync(filter);

            if (userProfile == null)
            {
                ModelState.AddModelError(nameof(userId), "user does not exist");
                return NotFound(ModelState);
            }

            return Ok(userProfile);
        }

        /// <summary>
        ///     Get User by User principal name
        /// </summary>
        [HttpGet("userName/{userName?}", Name = "GetUserByAdUserName")]
        [OpenApiOperation("GetUserByAdUserName")]
        [ProducesResponseType(typeof(UserProfile), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserByUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                ModelState.AddModelError(nameof(userName), "user principal name cannot be empty");
                return BadRequest(ModelState);
            }

            var filterText = userName.Replace("'", "''");
            var filter = $"userPrincipalName  eq '{filterText}'";

            var profile = new UserProfileHelper(_userAccountService);
            try
            {
                var userProfile = await profile.GetUserProfileAsync(filter);

                if (userProfile != null)
                {
                    return Ok(userProfile);
                }

                ModelState.AddModelError(nameof(userName), "user principal name does not exist");
                return NotFound(ModelState);

            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        ///     Get user profile by email
        /// </summary>
        [HttpGet("email/{**email}", Name = "GetUserByEmail")]
        [OpenApiOperation("GetUserByEmail")]
        [ProducesResponseType(typeof(UserProfile), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(nameof(email), "email cannot be empty");
                return BadRequest(ModelState);
            }

            if (!email.IsValidEmail())
            {
                ModelState.AddModelError(nameof(email), "email does not exist");
                return NotFound(ModelState);
            }

            var emailText = email.Replace("'", "''");
            var filter = $"otherMails/any(c:c eq '{emailText}')";
            var profile = new UserProfileHelper(_userAccountService);
            var userProfile = await profile.GetUserProfileAsync(filter);

            if (userProfile == null) return NotFound();

            return Ok(userProfile);
        }

        /// <summary>
        ///     DEPRECATED - Methods using this should use be replaced with the override version
        ///     of this method that takes a username.
        /// </summary>
        [HttpGet("judges", Name = "GetJudges")]
        [OpenApiOperation("GetJudges")]
        [ProducesResponseType(typeof(List<UserResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetJudges()
        {
            var adJudges = await _distributedCache.GetOrAddAsync(() => _userAccountService.GetJudgesAsync());

            if (adJudges == null || !adJudges.Any())
            {
                return Ok(new List<UserResponse>());
            }

            return Ok(adJudges);
        }

        /// <summary>
        ///     Gets a list of judges with the filtered username
        /// </summary>
        [HttpGet("judgesbyusername", Name = "GetJudgesByUsername")]
        [OpenApiOperation("GetJudgesByUsername")]
        [ProducesResponseType(typeof(List<UserResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetJudgesByUsername(string username)
        {
            var adJudges = await _userAccountService.GetJudgesAsync(username);

            if (adJudges == null || !adJudges.Any())
            {
                return Ok(new List<UserResponse>());
            }

            return Ok(adJudges);
        }

        /// <summary>
        ///     Get Ejudiciary Judges from AD filtered by username
        /// </summary>
        [HttpGet("ejudJudges", Name = "GetEjudiciaryJudgesByUsername")]
        [OpenApiOperation("GetEjudiciaryJudgesByUsername")]
        [ProducesResponseType(typeof(List<UserResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetEjudiciaryJudgesByUsername(string username)
        {
            var ejudiciaryJudges = await _userAccountService.GetEjudiciaryJudgesAsync(username);

            if (ejudiciaryJudges == null || !ejudiciaryJudges.Any())
            {
                return Ok(new List<UserResponse>());
            }

            return Ok(ejudiciaryJudges);
        }

        /// <summary>
        ///     Refresh Judge List Cache
        /// </summary>
        [HttpGet("judges/cache", Name = "RefreshJudgeCache")]
        [OpenApiOperation("RefreshJudgeCache")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> RefreshJudgeCache()
        {
            await _distributedCache.RefreshCacheAsync(() => _userAccountService.GetJudgesAsync());

            return Ok();
        }

        /// <summary>
        /// Delete an AAD user
        /// </summary>
        /// <returns>NoContent</returns>
        [HttpDelete( "username/{username}", Name = "DeleteUser")]
        [OpenApiOperation("DeleteUser")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteUser([FromRoute]string username)
        {
            try
            {
                await _userAccountService.DeleteUserAsync(username);
            }
            catch (UserDoesNotExistException)
            {
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// Update an accounts first and last name
        /// </summary>
        /// <param name="userId">AD Object ID for user</param>
        /// <param name="payload"></param>
        /// <returns></returns>
        [HttpPatch( "username/{userId:Guid}", Name = "UpdateUserAccount")]
        [OpenApiOperation("UpdateUserAccount")]
        [ProducesResponseType(typeof(UserResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateUserAccount([FromRoute]Guid userId, [FromBody] UpdateUserAccountRequest payload)
        {
            var result = await new UpdateUserAccountRequestValidation().ValidateAsync(payload);
            if (!result.IsValid)
            {
                foreach (var failure in result.Errors)
                    ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);

                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage)).ToList();
                _telemetryClient.TrackTrace(new TraceTelemetry(
                    $"UpdateUserAccount validation failed: {string.Join(Separator, errors)}",
                    SeverityLevel.Error));
                return BadRequest(ModelState);
            }
            
            try
            {
                var user = await _userAccountService.UpdateUserAccountAsync(userId, payload.FirstName, payload.LastName);
                var response = GraphUserMapper.MapToUserResponse(user);
                return Ok(response);
            }
            catch (UserDoesNotExistException e)
            {
                return NotFound(e.Message);
            }
        }
        
        /// <summary>
        ///     Reset password for an AAD user
        /// </summary>
        /// <returns>New password</returns>
        [HttpPatch(Name = "ResetUserPassword")]
        [OpenApiOperation("ResetUserPassword")]
        [ProducesResponseType(typeof(UpdateUserResponse), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ResetUserPassword([FromBody]string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                ModelState.AddModelError(nameof(username), "username cannot be empty");
                return BadRequest(ModelState);
            }

            var filterText = username.Replace("'", "''");
            var filter = $"userPrincipalName  eq '{filterText}'";
            var profile = new UserProfileHelper(_userAccountService);
            var userProfile = await profile.GetUserProfileAsync(filter);
            if (userProfile == null)
            {
                return NotFound();
            }
            
            var password = await _userAccountService.UpdateUserPasswordAsync(userProfile.UserName);
            
            return Ok(new UpdateUserResponse{NewPassword = password});
        }
    }
}
