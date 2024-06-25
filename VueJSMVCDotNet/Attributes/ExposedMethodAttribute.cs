namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// This attribute is used to expose a method to a javascript based called for a model. A static 
    /// call will be attached to the Model object, whereas a non-static call will be attached to an
    /// instance of the model and is used to perform operations on the model.Allow null response is used
    /// to indicate that the function can respond with null, otherwise null response is treated as an error.
    /// </summary>
    /// <remarks>
    /// Tag a method as being exposed to allow for it to be called from the javascript side as either 
    /// and instance method (non-static) or a non-instance method (static).
    /// </remarks>
    /// <param name="allowNullResponse">Set to true if a response can be null, if not, an exception will be thrown if null is returned</param>
    /// <param name="isSlow">Set true to tag the method as a slow method.  This means that this method will take a long enough time
    /// that the potential for the connection to timeout exists, so additional code calls will be made to handle 
    /// running the method in the background.</param>
    /// <param name="arrayElementType">Set to the type of element that is going to be supplied in a slow response array.</param>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ExposedMethodAttribute(bool allowNullResponse = false, bool isSlow = false, Type? arrayElementType = null) : Attribute
    {
        internal bool AllowNullResponse => allowNullResponse;
        internal bool IsSlow => isSlow||arrayElementType!=null;
        internal Type? ArrayElementType => arrayElementType;
    }
}
