using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal class ExposedMethodEndpoint<H, M>(ILogger? logger) :
        AModelEndpoint<H, M>(logger)
        where H : IModelHandler<M>
        where M : IModel
    {
        private const string SlowMethodIdKey = "instance";
        private readonly ConcurrentDictionary<Guid, SlowMethodInstance<H,M>> slowMethods = [];

        protected override IEnumerable<RouteEndpoint> ProduceEndpoints(IEnumerable<ModelRouteAttribute> routes)
            => typeof(H).GetMethods(Constants.METHOD_FLAGS)
                .Where(m => m.GetCustomAttribute<ExposedMethodAttribute>(false)!=null)
                .GroupBy(m => m.Name)
                .SelectMany(grp => {
                    var staticMethods = grp
                        .Where(m=>!Helper.IsExposedMethodInstance(m))
                        .Select(method => new InjectableMethod(method))
                        .ToArray();
                    var instanceMethods = grp
                        .Where(m => Helper.IsExposedMethodInstance(m))
                        .Select(method => new InjectableMethod(method))
                        .ToArray();
                    return routes.SelectMany(mra =>
                    {
                        IEnumerable<RouteEndpoint> result = [];
                        var slowPath = $"/{mra.Path}/{grp.Key}/";
                        if (staticMethods.Length>0)
                            result = result.Append(
                                new(
                                    requestDelegate: async (context) =>
                                    {
                                        var handler = ActivatorUtilities.CreateInstance<H>(context.RequestServices);
                                        var callback = await LocateMethodAsync(context, staticMethods, Logger);
                                        if (callback!=null)
                                            await InvokeMethodAsync(callback.Value.method, callback.Value.pars, context, handler, slowPath);
                                        else
                                            await ReturnNotFound(context);
                                    },
                                    routePattern: ProduceRoute(mra.Path, false, $"/{grp.Key}"),
                                    order: 0,
                                    metadata: new(
                                        new HttpMethodMetadata([HttpMethods.Post])
                                    ),
                                    displayName: $"Static Method call for {typeof(H).Name}.{grp.Key}"
                                )
                            );
                        if (instanceMethods.Length>0)
                            result = result.Append(
                                new(
                                    requestDelegate: async (context) =>
                                    {
                                        var handler = ActivatorUtilities.CreateInstance<H>(context.RequestServices);
                                        var callback = await LocateMethodAsync(context, instanceMethods, Logger);
                                        if (callback!=null)
                                            await InvokeMethodAsync(callback.Value.method, callback.Value.pars, context, handler, slowPath);
                                        else
                                            await ReturnNotFound(context);
                                    },
                                    routePattern: ProduceRoute(mra.Path, true, $"/{grp.Key}"),
                                    order: 0,
                                    metadata: new(
                                        new HttpMethodMetadata([HttpMethods.Post])
                                    ),
                                    displayName: $"Instance Method call for {typeof(H).Name}.{grp.Key}"
                                )
                            );
                        if (Array.Exists(staticMethods,m=>m.IsSlow) || Array.Exists(instanceMethods,m => m.IsSlow))
                        {
                            result = result.Append(
                                new(
                                    requestDelegate: async (context) =>
                                    {
                                        if (context.GetRouteValue(SlowMethodIdKey)==null
                                        || !slowMethods.TryGetValue((Guid)context.GetRouteValue(SlowMethodIdKey)!, out var slowMethodInstance)
                                        || slowMethodInstance.IsExpired) {
                                            slowMethods.TryRemove((Guid)context.GetRouteValue(SlowMethodIdKey)!, out _);
                                            await ReturnNotFound(context);
                                            return;
                                        }
                                        await slowMethodInstance.HandleRequest(context);
                                        if (slowMethodInstance.IsFinished)
                                            slowMethods.TryRemove((Guid)context.GetRouteValue(SlowMethodIdKey)!, out _);
                                    },
                                    routePattern: ProduceRoute(mra.Path,false, $"/{grp.Key}/{{{SlowMethodIdKey}:guid}}"),
                                    order:0,
                                    metadata: new(
                                        new HttpMethodMetadata(["PULL"])
                                    ),
                                    displayName: $"Slow Method callback for {typeof(H).Name}.{grp.Key}"
                                )
                            );
                        }
                        return result;
                    });
                });
        
        private async ValueTask InvokeMethodAsync(InjectableMethod method, object[] pars,HttpContext context, H instance, string slowBasePath)
        {
            if (method.IsSlow)
            {
                var slowID = Guid.NewGuid();
                if (slowMethods.TryAdd(slowID, new(method, pars, context, instance, Logger)))
                {
                    context.Response.ContentType= "text/text";
                    context.Response.StatusCode= 200;
                    await context.Response.WriteAsync($"{slowBasePath}{slowID}");
                }
                else
                {
                    context.Response.ContentType = "text/text";
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("Unable to instantiate slow method instance");
                }
            }
            else if (method.ReturnType == typeof(void))
            {
                await method.InvokeAsync<object, M>(instance, context, Logger, pars: pars);
                context.Response.ContentType= "application/json";
                context.Response.StatusCode= 200;
                await context.Response.WriteAsync("");
            }
            else if (method.ReturnType==typeof(string) && !method.IsArrayReturn)
            {
                (var tmp, var requestData) = await method.InvokeAsync<string, M>(instance, context, Logger, pars: pars);
                context.Response.StatusCode= 200;
                context.Response.ContentType= (tmp==null ? "application/json" : "text/text");
                await context.Response.WriteAsync((tmp??Utility.JsonEncode(tmp, requestData)));
            }
            else
                await Utility.JsonEncode<object>(context, method.InvokeAsync<object, M>(instance, context, Logger, pars: pars));
                
        }
    }
}
