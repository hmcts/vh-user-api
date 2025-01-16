using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UserApi.Client
{
    [ExcludeFromCodeCoverage]
    public partial class UserApiClient
    {
        public static UserApiClient GetClient(HttpClient httpClient)
        {
            var apiClient = new UserApiClient(httpClient)
            {
                ReadResponseAsString = true
            };
            
            return apiClient;
        }
        
        public static UserApiClient GetClient(string baseUrl, HttpClient httpClient)
        {
            var apiClient = GetClient(httpClient);
            apiClient.BaseUrl = baseUrl;
            return apiClient;
        }
        
        static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
        {
            settings.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            settings.WriteIndented = true;
            settings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            settings.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        }
    }
}