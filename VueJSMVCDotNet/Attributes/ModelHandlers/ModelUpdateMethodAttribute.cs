namespace VueJSMVCDotNet.Attributes.ModelHandlers
{
    /// <summary>
    /// Used to indicate the Update method for the model called to update its items, return bool. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModelUpdateMethodAttribute : Attribute
    {
    }
}
