namespace VueJSMVCDotNet.Attributes.ModelHandlers
{
    /// <summary>
    /// This attribute is used to tag a string parameter in a method as the model id
    /// This will cause the method to be created as an instance call instead of a static class call.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class ModelIDParameterAttribute
        : Attribute
    { }
}
