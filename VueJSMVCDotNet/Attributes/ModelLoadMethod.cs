using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Attributes
{
    /*
     * Used to tag the Load Method for a given model
     */
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModelLoadMethod : Attribute
    {
    }
}
