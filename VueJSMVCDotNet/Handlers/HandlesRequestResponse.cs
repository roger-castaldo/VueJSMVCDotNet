using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Handlers
{
    internal record HandlesRequestResponse(bool Result, object? State, string? CacheURL, IRequestHandler RequestHandler)
        : IHandlesRequestResponse;
}
