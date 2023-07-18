using System;
using System.Collections.Generic;
using System.Text;

namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to indicate that the property is not allowed to be null.  Which is used in the validate function. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ModelRequiredField : Attribute
    {
    }
}
