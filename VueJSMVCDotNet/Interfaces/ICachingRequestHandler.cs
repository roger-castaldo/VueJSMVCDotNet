using Microsoft.AspNetCore.Http;

namespace VueJSMVCDotNet.Interfaces
{
    internal interface ICachingRequestHandler : IRequestHandler
    {
        Task<ICachableResponse?> ProduceResponseAsync(HttpContext context, object? state);
    }
}
