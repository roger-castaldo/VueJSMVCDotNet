namespace VueJSMVCDotNet.Interfaces
{
    /// <summary>
    /// Houses the data that is provided for the current request being processed
    /// </summary>
    public interface IRequestData
    {
        /// <summary>
        /// The keys for the parameters supplied by the request
        /// </summary>
        IEnumerable<string> Keys { get; }
        /// <summary>
        /// Returns the value of a given request parameter as the type T
        /// </summary>
        /// <typeparam name="T">The type the parameter is expected to be</typeparam>
        /// <param name="key">The name of the parameter</param>
        /// <returns>The value of the parameter converted to the given type or throws an appropriate exception</returns>
        T GetValue<T>(string key);
        /// <summary>
        /// The session for the current request as returned by the ISessionFactory implementation
        /// </summary>
        ISecureSession Session { get; }
        /// <summary>
        /// Called to access features defined in the request
        /// </summary>
        /// <param name="feature">The type of feature</param>
        /// <returns>The value of the feature, if implemented</returns>
        object this[Type feature]{get;}
    }
}
