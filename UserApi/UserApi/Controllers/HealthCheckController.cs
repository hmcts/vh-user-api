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
            const bool SUCCESS = true;
            const bool UNSUCCESS = false;

            var response = new UserApiHealthResponse();
            response.AppVersion = GetApplicationVersion();
            try
            {
                //Check if user profile end point is accessible
                const string filter = "otherMails/any(c:c eq 'checkuser@test.com')";
                await _userAccountService.GetUserByFilterAsync(filter);
                response.UserAccessHealth.Successful = SUCCESS;
            }
            catch (UserServiceException e)
            {
                response.UserAccessHealth.Successful = UNSUCCESS;
                response.UserAccessHealth.Data = e.Data;
                response.UserAccessHealth.ErrorMessage = e.Message;
            }
           
            try
            {
                //Check if group by name end point is accessible
                const string group = "TestGroup";
                await _userAccountService.GetGroupByNameAsync(group);
                response.GroupAccessHealth.Successful = SUCCESS;
            }
            catch (UserServiceException e)
            {
                response.GroupAccessHealth.Successful = UNSUCCESS;
                response.GroupAccessHealth.Data = e.Data;
                response.GroupAccessHealth.ErrorMessage = e.Message;
            }

            if (response.HelthCheckSuccessful)
            {
                return Ok(response);
            }

            return StatusCode((int)HttpStatusCode.InternalServerError, response);
        }

        private ApplicationVersion GetApplicationVersion()
        {
            var applicationVersion = new ApplicationVersion()
            {
                FileVersion = GetExecutingAssemblyAttribute<AssemblyFileVersionAttribute>(a => a.Version),
                InformationVersion = GetExecutingAssemblyAttribute<AssemblyInformationalVersionAttribute>(a => a.InformationalVersion)
            };

            return applicationVersion;
        }

        private string GetExecutingAssemblyAttribute<T>(Func<T, string> value) where T : Attribute
        {
            T attribute = (T)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(T));
            return value.Invoke(attribute);
        }
    }
}