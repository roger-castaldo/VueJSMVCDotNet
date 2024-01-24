namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to tag the Load All Models method
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModelLoadAllMethod : Attribute
    {
    }
}
