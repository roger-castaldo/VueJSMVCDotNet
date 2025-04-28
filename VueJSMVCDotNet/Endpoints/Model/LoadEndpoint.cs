using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal class LoadEndpoint<H, M>(ILogger? logger) :
        AModelEndpoint<H, M>(logger)
        where H : IModelHandler<M>
        where M : IModel
    {
        protected override IEnumerable<RouteEndpoint> ProduceEndpoints(IEnumerable<ModelRouteAttribute> routes)
        {
            return routes.Select(mra => new RouteEndpoint(
                requestDelegate: async (context) =>
                {
                    if (!await ValidateAccessAsync(context, Logger, null, LoadSecurityChecks))
                        await ReturnInsecure(context);
                    else
                    {
                        var handler = ActivatorUtilities.CreateInstance<H>(context.RequestServices);
                        var result = await handler.LoadAsync(GetModelID(context));
                        if (object.Equals(result, default(M?)))
                            await ReturnModelNotFound(context);
                        else
                            await context.Response.WriteAsync(Utility.JsonEncode(result, await Helper.ExtractPartsAsync(context, Logger)));
                    }
                },
                routePattern: ProduceRoute(mra.Path, true),
                order: 0,
                metadata: new(
                    new HttpMethodMetadata([HttpMethods.Get]
                )),
                displayName: $"Load call for {typeof(M).Name}"
            ));
        }
    }
}
