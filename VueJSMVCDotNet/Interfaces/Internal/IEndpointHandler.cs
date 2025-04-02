using Microsoft.AspNetCore.Routing;

namespace VueJSMVCDotNet.Interfaces.Internal
{
    internal interface IEndpointHandler
    {
        IEnumerable<RouteEndpoint> AsEndpoints { get; }
    }
}
