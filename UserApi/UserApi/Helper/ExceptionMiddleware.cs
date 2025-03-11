using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Trace;
using UserApi.Common;

namespace UserApi.Helper;

public class ExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (BadRequestException ex)
        {
            TraceException("400 Exception", ex);
            await HandleExceptionAsync(httpContext, HttpStatusCode.BadRequest, ex);
        }
        catch (Exception ex)
        {
            TraceException("API Exception", ex);
            await HandleExceptionAsync(httpContext, HttpStatusCode.InternalServerError, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, HttpStatusCode statusCode, Exception exception)
    {
        context.Response.StatusCode = (int) statusCode;
        var sb = new StringBuilder(exception.Message);
        var innerException = exception.InnerException;
        while (innerException != null)
        {
            sb.Append($" {innerException.Message}");
            innerException = innerException.InnerException;
        }
        return context.Response.WriteAsJsonAsync(sb.ToString());
    }
    
    private static void TraceException(string eventTitle, Exception exception)
    {
        var activity = Activity.Current;

        if (activity == null)
            return;
        
        activity.RecordException(exception);
        activity.AddTag("Event", eventTitle);
    }
}