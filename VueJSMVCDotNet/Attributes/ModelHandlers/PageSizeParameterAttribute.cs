namespace VueJSMVCDotNet.Attributes.ModelHandlers
{
    /// <summary>
    /// Used to identify the parameter of a paged method that is used to pass the page size
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class PageSizeParameterAttribute
        : Attribute
    {
    }
}
