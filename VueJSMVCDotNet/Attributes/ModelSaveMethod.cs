using System;
using System.Collections.Generic;
using System.Text;

namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to mark the model save method which returns a bool 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModelSaveMethod : Attribute
    {
    }
}
