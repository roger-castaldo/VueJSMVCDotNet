using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to Ignore a property for model generation. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property,AllowMultiple=false)]
    public class ModelIgnoreProperty : Attribute
    {
    }
}
