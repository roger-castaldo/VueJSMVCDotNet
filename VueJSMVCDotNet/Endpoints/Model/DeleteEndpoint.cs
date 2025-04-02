using VueJSMVCDotNet.Interfaces;
using Microsoft.AspNetCore.Routing;
using VueJSMVCDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal class DeleteEndpoint<H, M>(ILogger? logger) :
        AModelEndpoint<H, M>(logger)
        where H : IModelHandler<M>
        where M : IModel
    {
        protected override IEnumerable<RouteEndpoint> ProduceEndpoints(IEnumerable<ModelRouteAttribute> routes)
        {
            var delMethod = Array.Find(typeof(H).GetMethods(Constants.METHOD_FLAGS), m => m.GetCustomAttribute<ModelDeleteMethodAttribute>(false)!=null);
            if (delMethod!=null)
            {
                var securityChecks = ExtractSecurityChecks(delMethod).ToArray();
                var injectableDelMethod = new InjectableMethod(delMethod);

                return routes.Select(mra => new RouteEndpoint(
                    requestDelegate: async (context) =>
                    {
                        if (!await ValidateAccessAsync(context, Logger, null, securityChecks))
                            await ReturnInsecure(context);
                        else
                        {
                            var handler = ActivatorUtilities.CreateInstance<H>(context.RequestServices);
                            await Utility.JsonEncode<bool>(context, injectableDelMethod.InvokeAsync<bool, M>(handler, context, Logger));
                        }
                    },
                    routePattern: ProduceRoute(mra.Path,true),
                    order: 0,
                    metadata: new(
                        new HttpMethodMetadata([HttpMethods.Delete]
                    )),
                    displayName: $"Delete call for {typeof(M).Name}"
                ));
            }
            return [];
        }
    }
}
