namespace VueJSMVCDotNet.Attributes.ModelHandlers
{
    /// <summary>
    /// This attribute is used to tag an IModel parameter as the current model instance to be loaded for a given method.
    /// This will cause the method to be created as an instance call instead of a static class call.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class ModelInstanceParameterAttribute
        : Attribute
    { }
}
