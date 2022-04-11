using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to tag the Load All Models method
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModelLoadAllMethod : Attribute
    {
    }
}
