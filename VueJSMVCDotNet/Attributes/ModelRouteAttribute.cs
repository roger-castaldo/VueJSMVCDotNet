namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to specify the route(path) to use for accessing the model 
    /// </summary>
    /// <remarks>
    /// Define the base route for the model that all rest paths will be built off of.
    /// </remarks>
    /// <param name="path">The base path for the model's rest calls</param>
    /// <param name="host">(Optional) specify a host that is used, in the case of using more than one host.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ModelRouteAttribute(string path, string host = "*") : Attribute
    {
        internal string Host => host;
        internal string Path => path;
    }
}
