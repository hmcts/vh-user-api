using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using UserApi.Common;

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
            catch (BadRequestException ex)
            {
                ApplicationLogger.TraceException(Common.Helpers.TraceCategory.APIException.ToString(), "400 Exception",
                    ex, null, null);
                await HandleExceptionAsync(httpContext, (int) HttpStatusCode.BadRequest, ex);
            }
            catch (Exception ex)
            {
                ApplicationLogger.TraceException(Common.Helpers.TraceCategory.APIException.ToString(), "API Exception",
                    ex, null, null);
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