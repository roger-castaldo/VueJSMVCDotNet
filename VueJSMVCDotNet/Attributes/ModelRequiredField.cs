using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Attributes
{
    /*
     * Used to indicate that the property is not allowed to be null.  Which is used in the validate function.
     */
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ModelRequiredField : Attribute
    {
        public ModelRequiredField() { }
    }
}
