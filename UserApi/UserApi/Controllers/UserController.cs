using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UserApi.Contract.Requests;
using UserApi.Contract.Responses;
using UserApi.Helper;
using UserApi.Mappers;
using UserApi.Security;
using UserApi.Services;
using UserApi.Validations;

namespace UserApi.Controllers;

[Produces("application/json")]
[Route("users")]
[ApiController]
public class UserController(IUserAccountService userAccountService, Settings settings, ILogger<UserController> logger) : ControllerBase
{
    private const string Separator = "; ";
    private static readonly ActivitySource ActivitySource = new("UserController");
    /// <summary>
    ///     Create a new hearings reforms user account
    /// </summary>
    /// <param name="request">Details of a new user</param>
    [HttpPost(Name = "CreateUser")]
    [OpenApiOperation("CreateUser")]
    [ProducesResponseType(typeof(NewUserResponse), (int) HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int) HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(NewUserErrorResponse), (int) HttpStatusCode.Conflict)]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        using var activity = ActivitySource.StartActivity("CreateUser", ActivityKind.Server);
        var result = new CreateUserRequestValidation().Validate(request);

        if (!result.IsValid)
        {
            foreach (var failure in result.Errors)
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);

            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage)).ToList();
            activity?.SetTag("validation.errors", string.Join(Separator, errors));
            logger.LogError("CreateUser validation failed: {Errors}", errors);
            return BadRequest(ModelState);
        }

        try
        {
            var adUserAccount =
                await userAccountService.CreateUserAsync(request.FirstName, request.LastName,
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
            return Conflict(new NewUserErrorResponse { Message = "User already exists", Code = "UserExists", Username = e.Username });
        }
        catch (InvalidEmailException e)
        {
            return Conflict(new NewUserErrorResponse { Message = e.Message, Code = "InvalidEmail", Email = e.Email });
        }
    }

    /// <summary>
    ///     Get User by AD User ID
    /// </summary>
    [HttpGet("{userId}", Name = "GetUserByAdUserId")]
    [OpenApiOperation("GetUserByAdUserId")]
    [ProducesResponseType(typeof(UserProfile), (int) HttpStatusCode.OK)]
    [ProducesResponseType((int) HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetUserByAdUserId(string userId)
    {
        var filterText = userId.Replace("'", "''");
        var filter = $"id  eq '{filterText}'";
        var profile = new UserProfileHelper(userAccountService, settings);
        var userProfile = await profile.GetUserProfileAsync(filter);

        if (userProfile == null)
            return NotFound("user does not exist");
        
        return Ok(userProfile);
    }

    /// <summary>
    ///     Get User by User principal name
    /// </summary>
    [HttpGet("userName/{userName?}", Name = "GetUserByAdUserName")]
    [OpenApiOperation("GetUserByAdUserName")]
    [ProducesResponseType(typeof(UserProfile), (int) HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int) HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int) HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetUserByUserName(string userName)
    {
        if (string.IsNullOrEmpty(userName))
        {
            ModelState.AddModelError(nameof(userName), "user principal name cannot be empty");
            return BadRequest(ModelState);
        }

        var filterText = userName.Replace("'", "''");
        var filter = $"userPrincipalName  eq '{filterText}'";

        var profile = new UserProfileHelper(userAccountService, settings);
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
    [ProducesResponseType(typeof(UserProfile), (int) HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int) HttpStatusCode.BadRequest)]
    [ProducesResponseType((int) HttpStatusCode.NotFound)]
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
        var profile = new UserProfileHelper(userAccountService, settings);
        var userProfile = await profile.GetUserProfileAsync(filter);

        if (userProfile == null) return NotFound();

        return Ok(userProfile);
    }

    /// <summary>
    ///     Gets a list of judges with the filtered username
    /// </summary>
    [HttpGet("judgesbyusername", Name = "GetJudgesByUsername")]
    [OpenApiOperation("GetJudgesByUsername")]
    [ProducesResponseType(typeof(List<UserResponse>), (int) HttpStatusCode.OK)]
    public async Task<IActionResult> GetJudgesByUsername(string username)
    {
        var adJudges = await userAccountService.GetJudgesAsync(username);

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
    [ProducesResponseType(typeof(List<UserResponse>), (int) HttpStatusCode.OK)]
    public async Task<IActionResult> GetEjudiciaryJudgesByUsername(string username)
    {
        var ejudiciaryJudges = await userAccountService.GetEjudiciaryJudgesAsync(username);

        if (ejudiciaryJudges == null || !ejudiciaryJudges.Any())
        {
            return Ok(new List<UserResponse>());
        }

        return Ok(ejudiciaryJudges);
    }

    /// <summary>
    /// Delete an AAD user
    /// </summary>
    /// <returns>NoContent</returns>
    [HttpDelete("username/{username}", Name = "DeleteUser")]
    [OpenApiOperation("DeleteUser")]
    [ProducesResponseType((int) HttpStatusCode.NoContent)]
    [ProducesResponseType((int) HttpStatusCode.NotFound)]
    public async Task<IActionResult> DeleteUser([FromRoute] string username)
    {
        try
        {
            await userAccountService.DeleteUserAsync(username);
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
    [HttpPatch("username/{userId:Guid}", Name = "UpdateUserAccount")]
    [OpenApiOperation("UpdateUserAccount")]
    [ProducesResponseType(typeof(UserResponse), (int) HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int) HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int) HttpStatusCode.NotFound)]
    public async Task<IActionResult> UpdateUserAccount([FromRoute] Guid userId, [FromBody] UpdateUserAccountRequest payload)
    {
        using var activity = ActivitySource.StartActivity("UpdateUserAccount", ActivityKind.Server);
        var result = await new UpdateUserAccountRequestValidation().ValidateAsync(payload);
        if (!result.IsValid)
        {
            foreach (var failure in result.Errors)
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);

            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage)).ToList();
            activity?.SetTag("validation.errors", $"UpdateUserAccount validation failed: {string.Join(Separator, errors)}");
            logger.LogError("Update User Account validation failed: {Errors}", errors);
            return BadRequest(ModelState);
        }

        try
        {
            var user = await userAccountService.UpdateUserAccountAsync(userId, payload.FirstName,
                payload.LastName, payload.ContactEmail);
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
    [ProducesResponseType(typeof(ValidationProblemDetails), (int) HttpStatusCode.BadRequest)]
    [ProducesResponseType((int) HttpStatusCode.NotFound)]
    public async Task<IActionResult> ResetUserPassword([FromBody] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            ModelState.AddModelError(nameof(username), "username cannot be empty");
            return BadRequest(ModelState);
        }

        var filterText = username.Replace("'", "''");
        var filter = $"userPrincipalName  eq '{filterText}'";
        var profile = new UserProfileHelper(userAccountService, settings);
        var userProfile = await profile.GetUserProfileAsync(filter);
        if (userProfile == null)
        {
            return NotFound();
        }

        var password = await userAccountService.UpdateUserPasswordAsync(userProfile.UserName);

        return Ok(new UpdateUserResponse {NewPassword = password});
    }
    
    
    /// <summary>
    ///     Add a user to a group
    /// </summary>
    [HttpPatch("user/group", Name = "AddUserToGroup")]
    [OpenApiOperation("AddUserToGroup")]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int) HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int) HttpStatusCode.NotFound)]
    public async Task<IActionResult> AddUserToGroup(AddUserToGroupRequest request)
    {
        using var activity = ActivitySource.StartActivity("AddUserToGroup", ActivityKind.Server);
        var result = new AddUserToGroupRequestValidation().Validate(request);

        if (!result.IsValid)
        {
            foreach (var failure in result.Errors)
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);

            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage)).ToList();
            activity?.AddTag("validation.errors", $"AddUserToGroupRequest validation failed: {string.Join(Separator, errors)}");
            logger.LogError("Add User To Group validation failed: {Errors}", errors);
            return ValidationProblem(ModelState);
        }

        var groupId = userAccountService.GetGroupIdFromSettings(request.GroupName);
        if (string.IsNullOrEmpty(groupId))
        {
            logger.LogError("Group not found: {Group}", request.GroupName);
            return NotFound(ModelState);
        }
        try
        {
            await userAccountService.AddUserToGroupAsync(request.UserId, groupId);
        }
        catch (UserServiceException e)
        {
            ModelState.AddModelError(nameof(request.UserId), e.Reason);
            return NotFound(ModelState);
        }

        return Accepted();
    }
}