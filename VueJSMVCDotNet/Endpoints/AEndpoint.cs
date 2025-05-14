using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Interfaces;

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

        protected EndpointMetadataCollection ProduceMetaData<H, M>(IEnumerable<string> httpMethods, MethodInfo method, params object[] additional)
            where H : IModelHandler<M>
            where M : IModel
            => ProduceMetaData<H, M>(httpMethods, [method], additional);
        protected EndpointMetadataCollection ProduceMetaData<H, M>(IEnumerable<string> httpMethods, IEnumerable<MethodInfo> methods, params object[] additional)
            where H : IModelHandler<M>
            where M : IModel
            => new(additional.Concat(typeof(H).GetCustomAttributes().OfType<Attribute>()
                .Concat(methods.SelectMany(method=>method.GetCustomAttributes().OfType<Attribute>()))
                .Where(att => !att.GetType().Namespace!.StartsWith(typeof(ASecurityCheckAttribute).Namespace!)))
                .DistinctBy(att=>att.GetType())
                .Append(new HttpMethodMetadata(httpMethods)));

        protected ValueTask ReturnModelNotFound(HttpContext context)
            => ReturnNotFound(context, "Model Not Found");
    }
}
