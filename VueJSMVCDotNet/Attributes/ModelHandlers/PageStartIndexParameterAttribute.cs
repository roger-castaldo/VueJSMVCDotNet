namespace VueJSMVCDotNet.Attributes.ModelHandlers
{
    /// <summary>
    /// Used to identify the parameter in a paged call to be used as the page start index
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class PageStartIndexParameterAttribute
        : Attribute
    {
    }
}
