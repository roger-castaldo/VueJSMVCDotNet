namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to specify the route(path) to use for accessing the model 
    /// </summary>
    /// <remarks>
    /// Define the base route for the model that all rest paths will be built off of.
    /// </remarks>
    /// <param name="path">The base path for the model's rest calls</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ModelRouteAttribute(string path) : Attribute
    {
        internal string Path => path;
    }
}
