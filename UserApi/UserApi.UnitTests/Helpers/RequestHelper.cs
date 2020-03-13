using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace UserApi.UnitTests.Helpers
{
    public static class RequestHelper
    {
        public static string SerialiseRequestToSnakeCaseJson(object request)
        {
            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };

            return JsonConvert.SerializeObject(request, new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                Formatting = Formatting.Indented
            });
        }

        public static T DeserialiseSnakeCaseJsonToResponse<T>(string response)
        {
            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };

            return JsonConvert.DeserializeObject<T>(response, new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                Formatting = Formatting.Indented
            });
        }

        public static HttpResponseMessage CreateHttpResponseMessage(object serializeObject, HttpStatusCode httpStatusCode)
        {
            return new HttpResponseMessage(httpStatusCode)
            {
                Content = new StringContent(JsonConvert.SerializeObject(serializeObject), System.Text.Encoding.UTF8, "application/json")
            };
        }

        public static HttpResponseMessage CreateHttpResponseMessage(string content, HttpStatusCode httpStatusCode)
        {
            return new HttpResponseMessage(httpStatusCode)
            {
                Content = new StringContent(content)
            };
        }
    }
}
