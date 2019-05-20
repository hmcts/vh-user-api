using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace UserApi
{
    /// <summary>
    /// Class to initialize the telemetry with the role of the application
    /// </summary>
    public class CloudRoleNameInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Cloud.RoleName = "vh-user-api";
        }
    }
}