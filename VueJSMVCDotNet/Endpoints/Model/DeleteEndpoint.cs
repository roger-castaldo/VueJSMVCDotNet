using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Interfaces;

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
                var injectableDelMethod = new InjectableMethod(delMethod, ExtractSecurityChecks(delMethod));

                return routes.Select(mra => new RouteEndpoint(
                    requestDelegate: async (context) =>
                    {
                        if (!await ValidateAccessAsync(context, Logger, null, injectableDelMethod.SecurityChecks))
                            await ReturnInsecure(context);
                        else
                        {
                            var handler = await CreateLoaderAsync(context);
                            await Utility.JsonEncode<bool>(context, injectableDelMethod.InvokeAsync<bool, M>(handler, context, Logger));
                        }
                    },
                    routePattern: ProduceRoute(mra.Path, true),
                    order: 0,
                    metadata: ProduceMetaData<H, M>(
                        [HttpMethods.Delete],
                        delMethod
                    ),
                    displayName: $"Delete call for {typeof(M).Name}"
                ));
            }
            return [];
        }
    }
}
