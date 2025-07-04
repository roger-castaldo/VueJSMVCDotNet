using Microsoft.AspNetCore.Routing;

namespace VueJSMVCDotNet.Endpoints
{
    internal static class RouteEndpointBuilderExtensions
    {
        public static RouteEndpointBuilder AddMetadataRange(this RouteEndpointBuilder builder, IEnumerable<object> metadata)
        {
            foreach (var item in metadata)
            {
                builder.Metadata.Add(item);
            }
            return builder;
        }
    }
}
