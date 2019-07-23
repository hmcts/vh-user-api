using System.Collections;

namespace UserApi.Contract.Responses
{
    public class UserApiHealthResponse
    {
        public HealthCheck UserAccessHealth { get; set; }
        public HealthCheck GroupAccessHealth { get; set; }

        public UserApiHealthResponse()
        {
            UserAccessHealth = new HealthCheck();
            GroupAccessHealth = new HealthCheck();
        }
    }
    
    public class HealthCheck
    {
        public bool Successful { get; set; }
        public string ErrorMessage { get; set; }
        public IDictionary Data { get; set; }
    }
}