using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal class ModelListEndpoint<H, M>(ILogger? logger) :
        AModelEndpoint<H, M>(logger)
        where H : IModelHandler<M>
        where M : IModel
    {
        protected override IEnumerable<RouteEndpoint> ProduceEndpoints(IEnumerable<ModelRouteAttribute> routes)
            => typeof(H).GetMethods(Constants.METHOD_FLAGS)
                .Where(m => m.GetCustomAttribute<ModelListMethodAttribute>(false)!=null)
                .GroupBy(m => m.Name)
                .SelectMany(grp =>
                {
                    var methods = grp.Select(m => new InjectableMethod(m, ExtractSecurityChecks(m))).ToArray();
                    return routes.Select(mra =>
                        new RouteEndpoint(
                            requestDelegate: async (context) =>
                            {
                                var handler = ActivatorUtilities.CreateInstance<H>(context.RequestServices);
                                var callback = await LocateMethodAsync(context, methods, Logger);
                                if (callback!=null)
                                {
                                    if (!await ValidateAccessAsync(context, Logger, null, callback.Value.method.SecurityChecks, false))
                                        await ReturnInsecure(context);
                                    else if (callback.Value.method.Method.GetCustomAttribute<ModelListMethodAttribute>()!.Paged)
                                        await Utility.JsonEncode<PagedResult<M>>(context, callback.Value.method.InvokeAsync<PagedResult<M>, M>(handler, context, Logger, pars: callback.Value.pars));
                                    else
                                        await Utility.JsonEncode<IEnumerable<M>>(context, callback.Value.method.InvokeAsync<IEnumerable<M>, M>(handler, context, Logger, pars: callback.Value.pars));
                                }
                                else
                                    await ReturnNotFound(context);
                            },
                            routePattern: ProduceRoute(mra.Path, false, $"/{grp.Key}"),
                            order: 0,
                            metadata: ProduceMetaData<H, M>(
                                [HttpMethods.Post],
                                methods.Select(method=>method.Method)
                            ),
                            displayName: $"List call for {typeof(H).Name}.{grp.Key}"
                        )
                    );
                });
    }
}
