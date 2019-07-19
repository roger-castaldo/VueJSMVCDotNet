using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Attributes
{
    /*
     * Used to mark the model save method which returns a bool
     */
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModelSaveMethod : Attribute
    {
    }
}
