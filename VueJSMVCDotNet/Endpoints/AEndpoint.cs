using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Endpoints.Filtering;
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

        protected static Endpoint BuildEndpoint<H, M>(RequestDelegate requestDelegate, RoutePattern routePattern, int order, string displayName, IEnumerable<string> httpMethods, MethodInfo method, params object[] additional)
            where H : IModelHandler<M>
            where M : IModel
            => BuildEndpoint<H,M>(requestDelegate, routePattern, order, displayName, httpMethods, [method], additional);
        protected static Endpoint BuildEndpoint<H, M>(RequestDelegate requestDelegate, RoutePattern routePattern, int order, string displayName, IEnumerable<string> httpMethods, IEnumerable<MethodInfo> methods, params object[] additional)
            where H : IModelHandler<M>
            where M : IModel
        {
            var builder = new RouteEndpointBuilder(
                null,
                routePattern: routePattern,
                order: order
            )
            {
                DisplayName = displayName
            };
            builder.AddMetadataRange(
                additional.Concat(methods.SelectMany(method => method.GetCustomAttributes().OfType<Attribute>()))
                    .Concat(typeof(H).GetCustomAttributes().OfType<Attribute>()
                    .Where(att => !att.GetType().Namespace!.StartsWith(typeof(ASecurityCheckAttribute).Namespace!)))
                    .Append(new HttpMethodMetadata(httpMethods))
            );

            builder.FilterFactories.Add((ctx, del) =>
                async (context) => await ((IEndpointFilter)context.HttpContext.RequestServices.GetRequiredService<FeatureGateFilter>()).InvokeAsync(context, del)
            );


            var delegateFactoryResult = RequestDelegateFactory.Create(requestDelegate, new()
            {
                EndpointBuilder = builder
            });

            builder.RequestDelegate = delegateFactoryResult.RequestDelegate;

            return builder.Build();
        }

        protected ValueTask ReturnModelNotFound(HttpContext context)
            => ReturnNotFound(context, "Model Not Found");
    }
}
