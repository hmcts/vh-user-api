using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UserApi.Helper;
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
                var email = _configuration.GetSection("Testing").GetSection("ExistingEmail").Value;
                //Check if user profile end point is accessible
                var filter = $"otherMails/any(c:c eq '{email}')";
                var profile = new UserProfileHelper(_userAccountService);
                var userProfile = await profile.GetUserProfile(filter);

                if (userProfile == null) return NotFound();

                //Check if group by name end point is accessible
                var valuesSection = _configuration.GetSection("Testing:ExistingGroups");
                var name = valuesSection.GetChildren().First().GetValue<string>("Displayname");
                var adGroup = await _userAccountService.GetGroupByName(name);

                if (adGroup == null) return NotFound();
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