using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Testing.Common.Helpers
{
    public static class ApiRequestHelper
    {
        public static HttpResponseMessage CreateHttpResponseMessage(Object serializeObject, HttpStatusCode httpStatusCode)
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
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            };
        }
        
        public static T Deserialise<T>(string response)
        {
            return JsonSerializer.Deserialize<T>(response, DefaultSystemTextJsonSerializerSettings());
        }
        
        public static string Serialise(object request)
        {
            return JsonSerializer.Serialize(request, DefaultSystemTextJsonSerializerSettings());
        }

        private static JsonSerializerOptions DefaultSystemTextJsonSerializerSettings()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };

            return options;
        }
    }
}