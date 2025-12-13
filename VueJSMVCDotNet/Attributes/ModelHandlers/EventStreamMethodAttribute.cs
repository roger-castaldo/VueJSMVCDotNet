namespace VueJSMVCDotNet.Attributes.ModelHandlers
{
    /// <summary>
    /// This attribute is used to expose a Server Side Event Streaming method.  A static 
    /// call will be attached to the Model object, whereas a non-static call will be attached to an
    /// instance of the model and is used to perform operations on the model.  It will also require an input of 
    /// both a ChannelWriter&lt;object&gt; and a CancellationToken for handling when the client disconnects and to provide 
    /// a method for outputting items to the stream.  All other parameters must be simple and able to exist inside a GET url call.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class EventStreamMethodAttribute : Attribute
    { }
}
