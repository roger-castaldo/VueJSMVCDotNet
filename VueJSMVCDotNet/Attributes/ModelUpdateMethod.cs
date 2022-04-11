using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to indicate the Update method for the model called to update its items, return bool. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method,AllowMultiple=false)]
    public class ModelUpdateMethod : Attribute
    {
    }
}
