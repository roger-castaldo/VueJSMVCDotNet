using Microsoft.Extensions.Primitives;

namespace VueJSMVCDotNet.Interfaces
{
    internal interface ICachableResponse
    {
        string Content { get; }
        string ContentType { get; }
        DateTime Timestamp { get; }
        IEnumerable<IChangeToken> ChangeTokens { get; }
    }
}
