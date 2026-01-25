namespace VueJSMVCDotNet.Attributes.ModelHandlers
{
    /// <summary>
    /// Used to tag the Load All Models method
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModelLoadAllMethodAttribute : Attribute
    {
    }
}
