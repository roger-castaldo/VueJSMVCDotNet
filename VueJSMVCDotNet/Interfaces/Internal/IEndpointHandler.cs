using Microsoft.AspNetCore.Http;

namespace VueJSMVCDotNet.Interfaces.Internal
{
    internal interface IEndpointHandler
    {
        IEnumerable<Endpoint> AsEndpoints { get; }
    }
}
