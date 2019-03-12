using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services;

namespace UserApi.Controllers
{
    [Produces("application/json")]
    [Route("healthcheck")]
    [ApiController]
    public class HealthCheckController : ControllerBase
    {
        private readonly IUserAccountService _userAccountService;
        private IConfiguration _configuration { get; }

        public HealthCheckController(IUserAccountService userAccountService, IConfiguration configuration)
        {
            _userAccountService = userAccountService;
            _configuration = configuration;
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
                var email = "checkuser@test.com";
                //Check if user profile end point is accessible
                var filter = $"otherMails/any(c:c eq '{email}')";
                await _userAccountService.GetUserByFilter(filter);
            }
            catch (UserServiceException)
            {
                ModelState.AddModelError("User", "GetUserByFilter unauthorized access to Microsoft Graph");
            }

            try
            {
                //Check if group by name end point is accessible
                await _userAccountService.GetGroupByName("TestGroup");
            }
            catch (UserServiceException)
            {
                ModelState.AddModelError("User", "GetGroupByName unauthorized access to Microsoft Graph");
            }

            var modelStateErrors = this.ModelState.Values.SelectMany(m => m.Errors);
            if (modelStateErrors.Any())
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ModelState);
            }

            return Ok();
        }
    }
}