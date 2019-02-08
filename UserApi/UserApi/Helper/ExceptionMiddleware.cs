using System;
using System.Net;
using System.Threading.Tasks;
using Hearings.Common;
using Microsoft.AspNetCore.Http;
using HearingsAPI.Client;

namespace UserApi.Helper
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (HearingApiException ex)
            {
                ApplicationLogger.TraceException(TraceCategory.HearingsApi, "API response error", ex, null, null);
                await HandleExceptionAsync(httpContext, ex.StatusCode, ex);
            }
            catch (Exception ex)
            {
                ApplicationLogger.TraceException(TraceCategory.General, "Unhandled exception", ex,null, null);
                await HandleExceptionAsync(httpContext, (int) HttpStatusCode.InternalServerError, ex);   
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, int statusCode, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
                   
            return context.Response.WriteAsync(exception.Message);
        }

    }
}