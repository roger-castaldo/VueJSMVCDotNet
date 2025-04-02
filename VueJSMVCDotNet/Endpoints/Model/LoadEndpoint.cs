using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal class LoadEndpoint<H, T>(ILogger? logger) : 
        AModelEndpoint<H, T>(logger)
        where H : IModelHandler<T>
        where T : IModel
    {
        protected override IEnumerable<RouteEndpoint> ProduceEndpoints(IEnumerable<ModelRouteAttribute> routes)
        {
            var securityChecks = ExtractSecurityChecks(
                    typeof(H).GetInterfaceMap(typeof(IModelHandler<T>)).InterfaceMethods
                    .First(m=>Equals(m.Name, nameof(IModelHandler<T>.LoadAsync)))
                ).ToArray();
            return routes.Select(mra => new RouteEndpoint(
                requestDelegate: async (context) =>
                {
                    if (!await ValidateAccessAsync(context,Logger,null,securityChecks))
                        await ReturnInsecure(context);
                    else
                    {
                        var handler = ActivatorUtilities.CreateInstance<H>(context.RequestServices);
                        var result = await handler.LoadAsync(GetModelID(context));
                        await context.Response.WriteAsync(Utility.JsonEncode(result, await Helper.ExtractPartsAsync(context,Logger)));
                    }
                },
                routePattern: ProduceRoute(mra.Path,true),
                order: 0,
                metadata: new(
                    new HttpMethodMetadata([HttpMethods.Get]
                )),
                displayName: $"Load call for {typeof(T).Name}"
            ));
        }
    }
}
