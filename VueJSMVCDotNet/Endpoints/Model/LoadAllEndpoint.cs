using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.Attributes;
using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal class LoadAllEndpoint<H, M>(ILogger? logger) :
        AModelEndpoint<H, M>(logger)
        where H : IModelHandler<M>
        where M : IModel
    {
        protected override IEnumerable<RouteEndpoint> ProduceEndpoints(IEnumerable<ModelRouteAttribute> routes)
        {
            var loadAllMethod = Array.Find(typeof(H).GetMethods(Constants.METHOD_FLAGS), m => m.GetCustomAttribute<ModelLoadAllMethodAttribute>(false)!=null);
            if (loadAllMethod!=null)
            {
                var securityChecks = ExtractSecurityChecks(loadAllMethod).ToArray();
                var injectableLoadAllMethod = new InjectableMethod(loadAllMethod);

                return routes.Select(mra => new RouteEndpoint(
                    requestDelegate: async (context) =>
                    {
                        if (!await ValidateAccessAsync(context, Logger, null, securityChecks,false))
                            await ReturnInsecure(context);
                        else
                        {
                            var handler = ActivatorUtilities.CreateInstance<H>(context.RequestServices);
                            await Utility.JsonEncode<object>(context, injectableLoadAllMethod.InvokeAsync<object, M>(handler, context, Logger));
                        }
                    },
                    routePattern: ProduceRoute(mra.Path, false),
                    order: 0,
                    metadata: new(
                        new HttpMethodMetadata([HttpMethods.Get]
                    )),
                    displayName: $"Load All call for {typeof(M).Name}"
                ));
            }
            return [];
        }
    }
}
