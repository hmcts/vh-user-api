using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NSwag.Annotations;
using UserApi.Caching;
using UserApi.Contract.Responses;
using UserApi.Security;
using UserApi.Services;

namespace UserApi.Controllers
{
    [Produces("application/json")]
    [ApiController]
    [AllowAnonymous]
    public class HealthCheckController : ControllerBase
    {
        private readonly IUserAccountService _userAccountService;
        private readonly ICache _distributedCache;

        public HealthCheckController(IUserAccountService userAccountService, ICache distributedCache)
        {
            _userAccountService = userAccountService;
            _distributedCache = distributedCache;
        }

        /// <summary>
        ///     Run a health check of the service
        /// </summary>
        /// <returns>Error if fails, otherwise OK status</returns>
        [HttpGet("HealthCheck/health")]
        [HttpGet("health/liveness")]
        [OpenApiOperation("CheckServiceHealth")]
        [ProducesResponseType(typeof(UserApiHealthResponse), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(UserApiHealthResponse), (int) HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Health()
        {
            const bool SUCCESS = true;
            const bool UNSUCCESS = false;

            var response = new UserApiHealthResponse();
            response.AppVersion = GetApplicationVersion();
            try
            {
                //Check if user profile end point is accessible
                const string filter = "otherMails/any(c:c eq 'checkuser@hmcts.net')";
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

            try
            {
                await _distributedCache.GetOrAddAsync(
                    () => Task.FromResult(JObject.FromObject(new object()).ToString()));
                response.DistributedCacheHealth.Successful = true;
            }
            catch (Exception ex)
            {
                response.DistributedCacheHealth.Successful = false;
                response.DistributedCacheHealth.Data = ex.Data;
                response.DistributedCacheHealth.ErrorMessage = ex.Message;
            }

            return response.HealthCheckSuccessful
                ? Ok(response)
                : StatusCode((int) HttpStatusCode.InternalServerError, response);
        }

        private ApplicationVersion GetApplicationVersion()
        {
            var applicationVersion = new ApplicationVersion()
            {
                FileVersion = GetExecutingAssemblyAttribute<AssemblyFileVersionAttribute>(a => a.Version),
                InformationVersion =
                    GetExecutingAssemblyAttribute<AssemblyInformationalVersionAttribute>(a => a.InformationalVersion)
            };

            return applicationVersion;
        }

        private string GetExecutingAssemblyAttribute<T>(Func<T, string> value) where T : Attribute
        {
            T attribute = (T) Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(T));
            return attribute == null ? null : value.Invoke(attribute);
        }
    }
}