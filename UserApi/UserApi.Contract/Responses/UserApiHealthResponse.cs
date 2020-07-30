using System.Collections;

namespace UserApi.Contract.Responses
{
    public class UserApiHealthResponse
    {
        public HealthCheck UserAccessHealth { get; set; }
        public HealthCheck GroupAccessHealth { get; set; }
        public ApplicationVersion AppVersion { get; set; }
        public HealthCheck DistributedCacheHealth { get; set; }
        
        public UserApiHealthResponse()
        {
            UserAccessHealth = new HealthCheck();
            GroupAccessHealth = new HealthCheck();
            AppVersion = new ApplicationVersion();
            DistributedCacheHealth = new HealthCheck();
        }

        public bool HealthCheckSuccessful => UserAccessHealth.Successful && GroupAccessHealth.Successful && DistributedCacheHealth.Successful;
    }
    
    public class HealthCheck
    {
        public bool Successful { get; set; }
        public string ErrorMessage { get; set; }
        public IDictionary Data { get; set; }
    }

    public class ApplicationVersion
    {
        public string FileVersion { get; set; }
        public string InformationVersion { get; set; }
    }
}