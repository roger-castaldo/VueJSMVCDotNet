using Microsoft.Extensions.Primitives;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Caching
{
    internal record CachableResponse(string Content, string ContentType, DateTime Timestamp, IEnumerable<IChangeToken> ChangeTokens)
        : ICachableResponse
    {
    }
}
