using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Attributes
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
        /// <param name="session">The secure Session passed to the Request Handler</param>
        /// <param name="model">The model in question (can be null if the model is not loaded at this point)</param>
        /// <param name="url">The url that was called for the request</param>
        /// <param name="parameters">Any parameters supplied with the request</param>
        /// <returns>true if the supplied session can access</returns>
        public abstract bool HasValidAccess(ISecureSession session,IModel model,string url, Hashtable parameters);
    }
}
