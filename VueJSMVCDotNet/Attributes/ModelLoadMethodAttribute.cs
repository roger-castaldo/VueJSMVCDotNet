namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to tag the Load Method for a given model 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModelLoadMethodAttribute : Attribute
    {
    }
}
