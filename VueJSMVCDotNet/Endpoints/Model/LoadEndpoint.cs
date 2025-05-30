using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using VueJSMVCDotNet.Attributes.ModelHandlers;
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
            var map = typeof(H).GetInterfaceMap(typeof(IModelHandler<M>));
            var loadMethod = map.TargetMethods[Array.FindIndex(map.InterfaceMethods, m => Equals(m.Name, nameof(IModelHandler<M>.LoadAsync)))];
            return routes.Select(mra => new RouteEndpoint(
                requestDelegate: async (context) =>
                {
                    if (!await ValidateAccessAsync(context, Logger, null, LoadSecurityChecks))
                        await ReturnInsecure(context);
                    else
                    {
                        var handler = await CreateLoaderAsync(context);
                        var result = await handler.LoadAsync(GetModelID(context));
                        if (object.Equals(result, default(M?)))
                            await ReturnModelNotFound(context);
                        else
                            await context.Response.WriteAsync(Utility.JsonEncode(result, await Helper.ExtractPartsAsync(context, Logger)));
                    }
                },
                routePattern: ProduceRoute(mra.Path, true),
                order: 0,
                metadata: ProduceMetaData<H, M>(
                        [HttpMethods.Get],
                        loadMethod
                ),
                displayName: $"Load call for {typeof(M).Name}"
            ));
        }
    }
}
