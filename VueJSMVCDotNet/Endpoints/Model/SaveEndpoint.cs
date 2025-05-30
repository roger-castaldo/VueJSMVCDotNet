using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Interfaces;

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
                var injectableSaveMethod = new InjectableMethod(saveMethod, ExtractSecurityChecks(saveMethod));

                return routes.Select(mra => new RouteEndpoint(
                    requestDelegate: async (context) =>
                    {
                        if (!await ValidateAccessAsync(context, Logger, null, injectableSaveMethod.SecurityChecks, false))
                            await ReturnInsecure(context);
                        else
                        {
                            var handler = await CreateLoaderAsync(context);
                            var data = await Helper.ExtractPartsAsync(context, Logger);
                            context.Response.ContentType = "application/json";
                            context.Response.StatusCode= 200;
                            await Utility.JsonEncode<string>(
                                context,
                                injectableSaveMethod.InvokeAsync<string, M>(handler, context, Logger, modelInstance: Utility.JsonDecode<M>(((ModelRequestData)data).RawBody!, data))
                            );
                        }
                    },
                    routePattern: ProduceRoute(mra.Path, false),
                    order: 0,
                    metadata: ProduceMetaData<H,M>(
                        [HttpMethods.Put], 
                        saveMethod
                    ),
                    displayName: $"Save call for {typeof(M).Name}"
                ));
            }
            return [];
        }
    }
}
