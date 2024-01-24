namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to specify the route(path) to use for accessing the model 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ModelRoute : Attribute
    {
        internal string Host { get; private init; }
        internal string Path { get; private init; }

        /// <summary>
        /// Define the base route for the model that all rest paths will be built off of.
        /// </summary>
        /// <param name="path">The base path for the model's rest calls</param>
        /// <param name="host">(Optional) specify a host that is used, in the case of using more than one host.</param>
        public ModelRoute(string path,string host="*")
        {
            Path = path;
            Host = host;
        }
    }
}
