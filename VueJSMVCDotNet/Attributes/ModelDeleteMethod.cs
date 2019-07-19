using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Attributes
{
    /*
     * Used to mark the Delete Method for a model which requires no parameters and to return bool on success or failure
     */
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModelDeleteMethod : Attribute
    {
    }
}
