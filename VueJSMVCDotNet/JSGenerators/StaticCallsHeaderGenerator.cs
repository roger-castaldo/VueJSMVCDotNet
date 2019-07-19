using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class StaticCallsHeaderGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, bool minimize, Type modelType)
        {
            builder.AppendLine(string.Format("var {0}={{}};", Constants.STATICS_VARAIBLE));
        }
    }
}
