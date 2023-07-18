using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// An abstract class used to implement Security Check attributes.  These can be tagged on 
    /// both classes and methods and will call HasValidAccess passing the session and 
    /// other relevant data any time anything inside the assigned class is access 
    /// or in the case of a method, any time that method is called.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Method,AllowMultiple=true,Inherited =false)]
    public abstract class ASecurityCheck : Attribute
    {
        /// <summary>
        /// Called to check if the current secure session has access to make the given call
        /// </summary>
        /// <param name="data">The extracted request data which includes the session and extracted parameters</param>
        /// <param name="model">The model in question (can be null if the model is not loaded at this point)</param>
        /// <param name="url">The url that was called for the request</param>
        /// <param name="id">the extract id of the model</param>
        /// <returns>true if the supplied session can access</returns>
        public abstract bool HasValidAccess(IRequestData data,IModel model,string url,string id);
    }
}
