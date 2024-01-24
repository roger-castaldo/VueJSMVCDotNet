namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to Ignore a property for model generation. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property,AllowMultiple=false)]
    public class ModelIgnoreProperty : Attribute
    {
    }
}
