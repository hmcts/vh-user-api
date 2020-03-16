using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using UserApi.Contract.Responses;
using UserApi.Security;
using UserApi.Services;

namespace UserApi.Controllers
{
    [Produces("application/json")]
    [Route("healthcheck")]
    [ApiController]
    [AllowAnonymous]
    public class HealthCheckController : ControllerBase
    {
        private readonly IUserAccountService _userAccountService;

        public HealthCheckController(IUserAccountService userAccountService)
        {
            _userAccountService = userAccountService;
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
            var response = new UserApiHealthResponse();
            response.AppVersion = GetApplicationVersion();
            try
            {
                const string email = "checkuser@test.com";
                //Check if user profile end point is accessible
                var filter = $"otherMails/any(c:c eq '{email}')";
                await _userAccountService.GetUserByFilterAsync(filter);
                response.UserAccessHealth.Successful = true;
            }
            catch (UserServiceException e)
            {
                response.UserAccessHealth.Successful = false;
                response.UserAccessHealth.Data = e.Data;
                response.UserAccessHealth.ErrorMessage = $"{e.Message} - {e.Reason}";
            }

            try
            {
                //Check if group by name end point is accessible
                await _userAccountService.GetGroupByNameAsync("TestGroup");
                response.GroupAccessHealth.Successful = true;
            }
            catch (UserServiceException e)
            {
                response.GroupAccessHealth.Successful = false;
                response.GroupAccessHealth.Data = e.Data;
                response.GroupAccessHealth.ErrorMessage = $"{e.Message} - {e.Reason}";
            }

            if (!response.GroupAccessHealth.Successful || !response.UserAccessHealth.Successful)
            {
                return StatusCode((int) HttpStatusCode.InternalServerError, response);
            }

            return Ok(response);
        }
        private ApplicationVersion GetApplicationVersion()
        {
            var applicationVersion = new ApplicationVersion();
            applicationVersion.FileVersion = GetExecutingAssemblyAttribute<AssemblyFileVersionAttribute>(a => a.Version);
            applicationVersion.InformationVersion = GetExecutingAssemblyAttribute<AssemblyInformationalVersionAttribute>(a => a.InformationalVersion);
            return applicationVersion;
        }

        private string GetExecutingAssemblyAttribute<T>(Func<T, string> value) where T : Attribute
        {
            T attribute = (T)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(T));
            return value.Invoke(attribute);
        }
    }
}