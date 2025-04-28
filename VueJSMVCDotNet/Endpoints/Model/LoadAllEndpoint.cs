using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

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
                var injectableLoadAllMethod = new InjectableMethod(loadAllMethod, ExtractSecurityChecks(loadAllMethod));

                return routes.Select(mra => new RouteEndpoint(
                    requestDelegate: async (context) =>
                    {
                        if (!await ValidateAccessAsync(context, Logger, null, injectableLoadAllMethod.SecurityChecks, false))
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
