using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Method,AllowMultiple=true,Inherited =false)]
    public abstract class ASecurityCheck : Attribute
    {
        public abstract bool HasValidAccess(ISecureSession session,IModel model,string url, Hashtable parameters);
    }
}
