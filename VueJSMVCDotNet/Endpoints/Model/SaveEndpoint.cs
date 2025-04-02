using VueJSMVCDotNet.Interfaces;
using Microsoft.AspNetCore.Routing;
using VueJSMVCDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal class SaveEndpoint<H, M>(ILogger? logger) :
        AModelEndpoint<H, M>(logger)
        where H : IModelHandler<M>
        where M : IModel
    {
        protected override IEnumerable<RouteEndpoint> ProduceEndpoints(IEnumerable<ModelRouteAttribute> routes)
        {
            var saveMethod = Array.Find(typeof(H).GetMethods(Constants.METHOD_FLAGS), m => m.GetCustomAttribute<ModelSaveMethodAttribute>(false)!=null);
            if (saveMethod!=null)
            {
                var securityChecks = ExtractSecurityChecks(saveMethod).ToArray();
                var injectableSaveMethod = new InjectableMethod(saveMethod);

                return routes.Select(mra => new RouteEndpoint(
                    requestDelegate: async (context) =>
                    {
                        if (!await ValidateAccessAsync(context, Logger, null, securityChecks,false))
                            await ReturnInsecure(context);
                        else
                        {
                            var handler = ActivatorUtilities.CreateInstance<H>(context.RequestServices);
                            var data = await Helper.ExtractPartsAsync(context, Logger);
                            context.Response.ContentType = "application/json";
                            context.Response.StatusCode= 200;
                            await Utility.JsonEncode<bool>(
                                context, 
                                injectableSaveMethod.InvokeAsync<bool, M>(handler, context, Logger, modelInstance: Utility.JsonDecode<M>(((ModelRequestData)data).RawBody!, data))
                            );
                        }
                    },
                    routePattern: ProduceRoute(mra.Path,false),
                    order: 0,
                    metadata: new(
                        new HttpMethodMetadata([HttpMethods.Put]
                    )),
                    displayName: $"Save call for {typeof(M).Name}"
                ));
            }
            return [];
        }
    }
}
