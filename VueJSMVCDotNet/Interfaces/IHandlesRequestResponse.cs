namespace VueJSMVCDotNet.Interfaces
{
    internal interface IHandlesRequestResponse
    {
        bool Result { get; }
        object? State { get; }
        string? CacheURL { get; }
        IRequestHandler RequestHandler { get; }
    }
}
