namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to mark the Delete Method for a model which requires no parameters and to return bool on success or failure
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModelDeleteMethod : Attribute
    {
    }
}
