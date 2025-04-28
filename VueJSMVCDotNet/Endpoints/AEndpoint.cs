using Microsoft.AspNetCore.Http;

namespace VueJSMVCDotNet.Endpoints
{
    internal abstract class AEndpoint(ILogger? logger)
    {
        protected ILogger? Logger => logger;
        protected async ValueTask ReturnInsecure(HttpContext context, string message = "Not Authorized")
        {
            logger?.LogError("Request Error, insecure access: {Message}", message);
            context.Response.ContentType = "text/text";
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync(message);
        }

        protected async ValueTask ReturnNotFound(HttpContext context, string message = "Not Found")
        {
            logger?.LogError("Request Error, not found: {URL}", context.Request.Path);
            context.Response.ContentType = "text/text";
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync(message);
        }

        protected async ValueTask ReturnModelNotFound(HttpContext context)
            => ReturnNotFound(context, "Model Not Found");
    }
}
