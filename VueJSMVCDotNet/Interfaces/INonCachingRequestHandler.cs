using Microsoft.AspNetCore.Http;

namespace VueJSMVCDotNet.Interfaces
{
    internal interface INonCachingRequestHandler
    {
        Task ProduceResponseAsync(HttpContext context, object? state);
    }
}
