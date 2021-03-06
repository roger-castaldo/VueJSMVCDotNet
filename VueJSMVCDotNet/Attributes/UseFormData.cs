using System;

namespace Org.Reddragonit.VueJSMVCDotNet.Attributes
{
    /*
     * This attribute is used to expose to flag a method call to use formdata for posting the data 
     * back to the server instead of JSON encoding which is used by default.  This is done in situations 
     * where JSON encoding the values might be a problem.
     */
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class UseFormData : Attribute{
        
    }
}