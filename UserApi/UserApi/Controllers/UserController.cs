using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    [Route("users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IUserAccountService _userAccountService;

        public UserController(IUserAccountService userAccountService, TelemetryClient telemetryClient)
        {
            _userAccountService = userAccountService;
            _telemetryClient = telemetryClient;
        }

        /// <summary>
        ///     Create a new hearings reforms user account
        /// </summary>
        /// <param name="request">Details of a new user</param>
        [HttpPost(Name = "CreateUser")]
        [SwaggerOperation(OperationId = "CreateUser")]
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
                    $"CreateUserRequest validation failed: {string.Join("; ", errors)}",
                    SeverityLevel.Error));
                return BadRequest(ModelState);
            }

            try
            {
                var adUserAccount =
                    await _userAccountService.CreateUserAsync(request.FirstName, request.LastName,
                        request.RecoveryEmail);
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
        }

        /// <summary>
        ///     Get User by AD User ID
        /// </summary>
        [HttpGet("{userId?}", Name = "GetUserByAdUserId")]
        [SwaggerOperation(OperationId = "GetUserByAdUserId")]
        [ProducesResponseType(typeof(UserProfile), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserByAdUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError(nameof(userId), "username cannot be empty");
                return BadRequest(ModelState);
            }

            var filter = $"objectId  eq '{userId}'";
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
        [SwaggerOperation(OperationId = "GetUserByAdUserName")]
        [ProducesResponseType(typeof(UserProfile), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserByUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                ModelState.AddModelError(nameof(userName), "user principal name cannot be empty");
                return BadRequest(ModelState);
            }

            var filter = $"userPrincipalName  eq '{userName}'";
            var profile = new UserProfileHelper(_userAccountService);
            var userProfile = await profile.GetUserProfileAsync(filter);

            if (userProfile == null)
            {
                ModelState.AddModelError(nameof(userName), "user principal name does not exist");
                return NotFound(ModelState);
            }

            return Ok(userProfile);
        }

        /// <summary>
        ///     Get user profile by email
        /// </summary>
        [HttpGet("email/{email?}", Name = "GetUserByEmail")]
        [SwaggerOperation(OperationId = "GetUserByEmail")]
        [ProducesResponseType(typeof(UserProfile), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(nameof(email), "email cannot be empty");
                return BadRequest(ModelState);
            }

            if (!(new EmailAddressAttribute().IsValid(email)))
            {
                ModelState.AddModelError(nameof(email), "email does not exist");
                return NotFound(ModelState);
            }

            var filter = $"otherMails/any(c:c eq '{email}')";
            var profile = new UserProfileHelper(_userAccountService);
            var userProfile = await profile.GetUserProfileAsync(filter);

            if (userProfile == null) return NotFound();

            return Ok(userProfile);
        }

        /// <summary>
        ///     Get Judges from AD
        /// </summary>
        [HttpGet("judges", Name = "GetJudges")]
        [SwaggerOperation(OperationId = "GetJudges")]
        [ProducesResponseType(typeof(List<UserResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetJudges()
        {
            var adJudges = await _userAccountService.GetJudgesAsync();
            if (adJudges == null || !adJudges.Any()) return Ok(new List<UserResponse>());

            return Ok(adJudges);
        }
    }
}