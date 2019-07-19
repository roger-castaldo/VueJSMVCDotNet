using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Attributes
{
    /*
     * Used to tag the Load All Models method
     */
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModelLoadAllMethod : Attribute
    {
    }
}
