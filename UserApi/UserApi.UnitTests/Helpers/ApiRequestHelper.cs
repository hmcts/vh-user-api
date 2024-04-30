using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;

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
    }
}