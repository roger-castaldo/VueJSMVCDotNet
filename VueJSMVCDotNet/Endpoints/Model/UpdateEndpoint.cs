using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Attributes.Models;
using VueJSMVCDotNet.Extensions;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal class UpdateEndpoint<H, M>(ILogger? logger) :
        AModelEndpoint<H, M>(logger)
        where H : IModelHandler<M>
        where M : IModel
    {
        protected override IEnumerable<Endpoint> ProduceEndpoints(IEnumerable<ModelRouteAttribute> routes)
        {
            var updateMethod = Array.Find(typeof(H).GetMethods(Constants.METHOD_FLAGS), m => m.GetCustomAttribute<ModelUpdateMethodAttribute>(false)!=null);
            if (updateMethod!=null)
            {
                var injectableUpdateMethod = new InjectableMethod(updateMethod, ExtractSecurityChecks(updateMethod));

                return routes.Select(mra => BuildEndpoint<H,M>(
                    requestDelegate: async (context) =>
                    {
                        if (!await ValidateAccessAsync(context,Logger,null, LoadSecurityChecks) ||
                            !await ValidateAccessAsync(context, Logger, null, injectableUpdateMethod.SecurityChecks))
                            await ReturnInsecure(context);
                        else
                        {
                            var handler = await CreateLoaderAsync(context);
                            var model = await handler.LoadAsync(GetModelID(context));
                            if (object.Equals(model, default(M?)))
                                await ReturnModelNotFound(context);
                            else
                            {
                                var data = await Helper.ExtractPartsAsync(context, Logger);
                                data.Keys.Where(key => !string.Equals(key, "id", StringComparison.InvariantCulture))
                                .Select(key => typeof(M).GetProperty(key))
                                .Where(pi => pi != null && pi.CanWrite
                                            && pi.GetCustomAttribute<ReadOnlyModelPropertyAttribute>(true)==null
                                )
                                .ForEach(pi =>
                                {
                                    Logger?.LogTrace("Attempting to convert the value supplied for property {FullName}.{Name} to {PropertyType}", typeof(M).FullName, pi!.Name, pi.PropertyType);
                                    pi!.SetValue(model, data.GetValue(pi.PropertyType, pi.Name));
                                });
                                context.Response.ContentType = "application/json";
                                context.Response.StatusCode= 200;
                                await Utility.JsonEncode<bool>(context, injectableUpdateMethod.InvokeAsync<bool, M>(handler, context, Logger, modelInstance: model));
                            }
                        }
                    },
                    routePattern: ProduceRoute(mra.Path, true),
                    order: 0,
                    displayName: $"Update call for {typeof(M).Name}",
                    httpMethods:[HttpMethods.Patch],
                    method:updateMethod
                ));
            }
            return [];
        }
    }
}
