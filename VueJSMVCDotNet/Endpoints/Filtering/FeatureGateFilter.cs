using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

namespace VueJSMVCDotNet.Endpoints.Filtering
{
    internal class FeatureGateFilter() : IEndpointFilter
    {
        async ValueTask<object?> IEndpointFilter.InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var featureManager = (IFeatureManager?)context.HttpContext.RequestServices.GetService(typeof(IFeatureManager));
            if (featureManager!=null)
            {
                var featureGates = context.HttpContext.GetEndpoint()?.Metadata.GetOrderedMetadata<FeatureGateAttribute>();
                if (featureGates is not null)
                {
                    foreach (var metadata in featureGates)
                    {
                        var isValid = false;
                        foreach (var feature in metadata.Features)
                        {
                            (isValid, var exit) = (await featureManager.IsEnabledAsync(feature), metadata.Negate, metadata.RequirementType) switch
                            {
                                (true, false, RequirementType.Any) => (true, true),
                                (true, false, RequirementType.All) => (true, false),
                                (false, true, RequirementType.Any) => (true, false),
                                (false, true, RequirementType.All) => (true, false),
                                (false, false, RequirementType.Any) => (false, false),
                                _ => (false, true)
                            };
                            if (exit)
                                break;
                        }
                        if (!isValid)
                            return Results.NotFound();
                    }
                }
            }
            return await next(context);
        }
    }
}
