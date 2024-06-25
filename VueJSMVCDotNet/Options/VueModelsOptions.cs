using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Options
{
    /// <summary>
    /// Used to supply the additional components for the VueModelsHandler
    /// </summary>
    public record VueModelsOptions
    {
        /// <summary>
        /// The Secure Session Factory builder
        /// </summary>
        public ISecureSessionFactory? SessionFactory { get; init; } = null;
        /// <summary>
        /// Options: This will remap all urls provided in attributes to the base path provided (e.g. "/modules/tester/")
        /// </summary>
        public string? BaseURL { get; init; } = null;
        /// <summary>
        /// Optional: If flagged as true it will ignore/disable invalid models
        /// </summary>
        public bool IgnoreInvalidModels { get; init; } = false;
        /// <summary>
        /// Optional:  A list of header keys to read from and write to for all requests if using headers for security
        /// </summary>
        public IEnumerable<string> SecurityHeaders { get; init; } = [];
    }
}
