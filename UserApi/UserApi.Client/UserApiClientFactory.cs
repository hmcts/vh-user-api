using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace UserApi.Client
{
    public partial class UserApiClient
    {
        public static UserApiClient GetClient(HttpClient httpClient)
        {
            var apiClient = new UserApiClient(httpClient)
            {
                ReadResponseAsString = true
            };
            apiClient.JsonSerializerSettings.ContractResolver = new DefaultContractResolver {NamingStrategy = new SnakeCaseNamingStrategy()};
            apiClient.JsonSerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            apiClient.JsonSerializerSettings.Converters.Add(new StringEnumConverter());
            return apiClient;
        }
        
        public static UserApiClient GetClient(string baseUrl, HttpClient httpClient)
        {
            var apiClient = GetClient(httpClient);
            apiClient.BaseUrl = baseUrl;
            return apiClient;
        }
    }
}