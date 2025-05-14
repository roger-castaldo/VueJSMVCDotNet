namespace VueJSMVCDotNet.Attributes.Models
{
    /// <summary>
    /// Used to Ignore a property for model generation. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ModelIgnorePropertyAttribute : Attribute
    {
    }
}
