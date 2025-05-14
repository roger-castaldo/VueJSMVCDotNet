namespace VueJSMVCDotNet.Attributes.Models
{
    /// <summary>
    /// Used to indicate that the property is not allowed to be null.  Which is used in the validate function. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ModelRequiredFieldAttribute : Attribute
    {
    }
}
