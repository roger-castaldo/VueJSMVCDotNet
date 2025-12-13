using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using System.Collections.Concurrent;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal class ExposedMethodEndpoint<H, M>(ILogger? logger) :
        AModelEndpoint<H, M>(logger)
        where H : IModelHandler<M>
        where M : IModel
    {
        private const string NotFoundError = "Unable to locate method with matching parameters";
        private const string SlowMethodIdKey = "instance";
        private readonly ConcurrentDictionary<Guid, SlowMethodInstance<H, M>> slowMethods = [];

        protected override IEnumerable<Endpoint> ProduceEndpoints(IEnumerable<ModelRouteAttribute> routes)
            => typeof(H).GetMethods(Constants.METHOD_FLAGS)
                .Where(m => m.GetCustomAttribute<ExposedMethodAttribute>(false)!=null)
                .GroupBy(m => m.Name)
                .SelectMany(grp =>
                {
                    var staticMethods = grp
                        .Where(m => !Helper.IsExposedMethodInstance(m))
                        .Select(method => new InjectableMethod(method, ExtractSecurityChecks(method)))
                        .ToArray();
                    var instanceMethods = grp
                        .Where(m => Helper.IsExposedMethodInstance(m))
                        .Select(method => new InjectableMethod(method, ExtractSecurityChecks(method)))
                        .ToArray();
                    return routes.SelectMany(mra =>
                    {
                        IEnumerable<Endpoint> result = [];
                        var slowPath = $"{(mra.Path.StartsWith('/') ? "" : "/")}{mra.Path}/{grp.Key}/";
                        if (staticMethods.Length>0)
                            result = result.Append(
                                BuildEndpoint<H, M>(
                                    requestDelegate: async (context) =>
                                    {
                                        var handler = await CreateLoaderAsync(context);
                                        var callback = await LocateMethodAsync(context, staticMethods, Logger);
                                        if (callback!=null)
                                        {
                                            if (!await ValidateAccessAsync(context, Logger, null, callback.Value.method.SecurityChecks, false))
                                                await ReturnInsecure(context);
                                            else
                                                await InvokeMethodAsync(callback.Value.method, callback.Value.pars, context, handler, slowPath);
                                        }
                                        else
                                            await ReturnNotFound(context, NotFoundError);
                                    },
                                    routePattern: ProduceRoute(mra.Path, false, $"/{grp.Key}"),
                                    order: 0,
                                    displayName: $"Static Method call for {typeof(H).Name}.{grp.Key}",
                                    httpMethods: [HttpMethods.Post],
                                    methods: staticMethods.Select(method => method.Method)

                                )
                            );
                        if (instanceMethods.Length>0)
                            result = result.Append(
                                BuildEndpoint<H, M>(
                                    requestDelegate: async (context) =>
                                    {
                                        if (!await ValidateAccessAsync(context, Logger, null, LoadSecurityChecks))
                                            await ReturnInsecure(context);
                                        else
                                        {
                                            var handler = await CreateLoaderAsync(context);
                                            var callback = await LocateMethodAsync(context, instanceMethods, Logger);
                                            if (callback!=null)
                                            {
                                                if (!await ValidateAccessAsync(context, Logger, null, callback.Value.method.SecurityChecks))
                                                    await ReturnInsecure(context);
                                                else
                                                {
                                                    var modelInstance = await handler.LoadAsync((await Helper.ExtractPartsAsync(context, Logger)).ModelID!);
                                                    if (object.Equals(modelInstance, default(M?)))
                                                        await ReturnModelNotFound(context);
                                                    else
                                                        await InvokeMethodAsync(callback.Value.method, callback.Value.pars, context, handler, slowPath, modelInstance: modelInstance);
                                                }
                                            }
                                            else
                                                await ReturnNotFound(context, NotFoundError);
                                        }
                                    },
                                    routePattern: ProduceRoute(mra.Path, true, $"/{grp.Key}"),
                                    order: 0,
                                    displayName: $"Instance Method call for {typeof(H).Name}.{grp.Key}",
                                    httpMethods: [HttpMethods.Post],
                                    methods: instanceMethods.Select(method => method.Method)
                                )
                            );
                        if (Array.Exists(staticMethods, m => m.IsSlow) || Array.Exists(instanceMethods, m => m.IsSlow))
                        {
                            result = result.Append(
                                new RouteEndpoint(
                                    requestDelegate: async (context) =>
                                    {
                                        if (context.GetRouteValue(SlowMethodIdKey)==null)
                                        {
                                            await ReturnNotFound(context);
                                            return;
                                        }
                                        var idKey = Guid.Parse(context.GetRouteValue(SlowMethodIdKey)!.ToString()!);
                                        if (!slowMethods.TryGetValue(idKey, out var slowMethodInstance)
                                        || slowMethodInstance.IsExpired)
                                        {
                                            slowMethods.TryRemove(idKey, out _);
                                            await ReturnNotFound(context);
                                            return;
                                        }
                                        await slowMethodInstance.HandleRequest(context);
                                        if (slowMethodInstance.IsFinished)
                                        {
                                            slowMethods.TryRemove(idKey, out var instance);
                                            instance?.Dispose();
                                        }
                                    },
                                    routePattern: RoutePatternFactory.Parse($"{slowPath}{{{SlowMethodIdKey}}}"),
                                    order: 0,
                                    metadata: new(
                                        new HttpMethodMetadata([HttpMethods.Get])
                                    ),
                                    displayName: $"Slow Method callback for {typeof(H).Name}.{grp.Key}"
                                )
                            );
                        }
                        return result;
                    });
                });

        private const string TextContentType = "text/text";
        private const string JsonContentType = "application/json";

        private async ValueTask InvokeMethodAsync(InjectableMethod method, object?[] pars, HttpContext context, H instance, string slowBasePath, M? modelInstance = default)
        {
            try
            {
                if (method.IsSlow)
                {
                    var slowID = Guid.NewGuid();
                    if (slowMethods.TryAdd(slowID, new(method, pars, await Helper.ExtractPartsAsync(context, Logger), instance, Logger, modelInstance)))
                    {
                        context.Response.ContentType= TextContentType;
                        context.Response.StatusCode= 200;
                        await context.Response.WriteAsync($"{slowBasePath}{slowID}");
                    }
                    else
                    {
                        context.Response.ContentType = TextContentType;
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("Unable to instantiate slow method instance");
                    }
                }
                else if (method.ReturnType == typeof(void))
                {
                    await method.InvokeAsync<object, M>(instance, context, Logger, pars: pars, modelInstance: modelInstance);
                    context.Response.ContentType= JsonContentType;
                    context.Response.StatusCode= 200;
                    await context.Response.WriteAsync("");
                }
                else if (method.ReturnType==typeof(string) && !method.IsArrayReturn)
                {
                    (var tmp, var requestData) = await method.InvokeAsync<string, M>(instance, context, Logger, pars: pars, modelInstance: modelInstance);
                    context.Response.StatusCode= 200;
                    context.Response.ContentType= (tmp==null ? JsonContentType : TextContentType);
                    await context.Response.WriteAsync((tmp??Utility.JsonEncode(tmp, requestData)));
                }
                else
                    await Utility.JsonEncode<object>(context, method.InvokeAsync<object, M>(instance, context, Logger, pars: pars, modelInstance: modelInstance));
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error invoking exposed method {MethodName}", method.Name);
                context.Response.StatusCode = 500;
                context.Response.ContentType = TextContentType;
                await context.Response.WriteAsync("Internal Server Error");
            }
        }
    }
}
