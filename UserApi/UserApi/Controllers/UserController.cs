using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Swashbuckle.AspNetCore.Annotations;
using UserApi.Common;
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
        private IConfiguration _configuration { get; }

        public UserController(IUserAccountService userAccountService, TelemetryClient telemetryClient, IConfiguration configuration)
        {
            _userAccountService = userAccountService;
            _telemetryClient = telemetryClient;
            _configuration = configuration;
        }

        /// <summary>
        ///     Create a new hearings reforms user account
        /// </summary>
        /// <param name="request">Details of a new user</param>
        [HttpPost(Name = "CreateUser")]
        [SwaggerOperation(OperationId = "CreateUser")]
        [ProducesResponseType(typeof(NewUserResponse), (int) HttpStatusCode.Created)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
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

            var adUserAccount = await _userAccountService.CreateUser(request.FirstName, request.LastName);
            await _userAccountService.UpdateAuthenticationInformation(adUserAccount.UserId, request.RecoveryEmail);

            var response = new NewUserResponse
            {
                UserId = adUserAccount.UserId,
                Username = adUserAccount.Username,
                OneTimePassword = adUserAccount.OneTimePassword
            };
            return CreatedAtRoute("GetUserByAdUserId", new {userId = adUserAccount.UserId}, response);
        }

        /// <summary>
        ///     Get User by AD User ID
        /// </summary>
        [HttpGet("{userId}", Name = "GetUserByAdUserId")]
        [SwaggerOperation(OperationId = "GetUserByAdUserId")]
        [ProducesResponseType(typeof(UserProfile), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserByAdUserId(string userId)
        {
            var filter = $"objectId  eq '{userId}'";
            var profile = new UserProfileHelper(_userAccountService);
            var userProfile = await profile.GetUserProfile(filter);

            if (userProfile == null) return NotFound();

            return Ok(userProfile);
        }

        /// <summary>
        ///     Get User by User principle name
        /// </summary>
        [HttpGet("userName/{userName}", Name = "GetUserByAdUserName")]
        [SwaggerOperation(OperationId = "GetUserByAdUserName")]
        [ProducesResponseType(typeof(UserProfile), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserByUserName(string userName)
        {
            var filter = $"userPrincipalName  eq '{userName}'";
            var profile = new UserProfileHelper(_userAccountService);
            var userProfile = await profile.GetUserProfile(filter);

            if (userProfile == null) return NotFound();

            return Ok(userProfile);
        }

        /// <summary>
        ///     Get user profile by email
        /// </summary>
        [HttpGet("email/{email}", Name = "GetUserByEmail")]
        [SwaggerOperation(OperationId = "GetUserByEmail")]
        [ProducesResponseType(typeof(UserProfile), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var filter = $"otherMails/any(c:c eq '{email}')";
            var profile = new UserProfileHelper(_userAccountService);
            var userProfile = await profile.GetUserProfile(filter);

            if (userProfile == null) return NotFound();

            return Ok(userProfile);
        }

        /// <summary>
        ///     Run a health check of the service
        /// </summary>
        /// <returns>Error if fails, otherwise OK status</returns>
        [HttpGet("health")]
        [SwaggerOperation(OperationId = "CheckServiceHealth")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Health()
        {
            try
            {
                var email = _configuration.GetSection("Health").GetSection("HealthCheckEmail").Value;
                //Check if the end point is accessible
                var filter = $"otherMails/any(c:c eq '{email}')";
                var profile = new UserProfileHelper(_userAccountService);
                var userProfile = await profile.GetUserProfile(filter);

                if (userProfile == null) return NotFound();
            }
            catch (Exception ex)
            {
                var data = new
                {
                    ex.Message,
                    ex.Data
                };
                return StatusCode((int)HttpStatusCode.InternalServerError, data);
            }

            return Ok();
        }
    }
}