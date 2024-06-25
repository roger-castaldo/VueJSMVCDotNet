namespace VueJSMVCDotNet.Options
{
    /// <summary>
    /// Used to supply the additional components for the MessagesHandler
    /// </summary>
    /// 
    public record MessageHandlerOptions
    {
        /// <summary>
        /// The base url for the messages to exist inside
        /// </summary>
        public string BaseURL { get; init; } = string.Empty;
    }
}
