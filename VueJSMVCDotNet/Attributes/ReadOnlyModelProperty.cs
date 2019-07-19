using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Attributes
{
    /*
     * Used to specify an uneditable readonly property for a given model
     */
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ReadOnlyModelProperty : Attribute
    {
    }
}
