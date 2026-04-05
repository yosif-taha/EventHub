using EventHub.Application.Common.Responses;
using EventHub.WebAPI.Presentation.ViewModels.Respponse;

namespace EventHub.WebAPI.Presentation.Middlewares
{
    public class GlobalErrorHandlerMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }
        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new FailedResponseViewModel(
                ErrorCode.InternalServerError,
                "An internal server error occurred. Please try again later."
            );

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
