namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to specify an uneditable readonly property for a given model 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ReadOnlyModelProperty : Attribute
    {
    }
}
