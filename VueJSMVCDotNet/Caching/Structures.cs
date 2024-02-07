namespace VueJSMVCDotNet.Caching
{
    internal record CachedContent
    {
        public DateTime Timestamp { get; init; }
        public string Content { get; init; }
    }
}
