using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class FooterGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, bool minimize, Type modelType)
        {
            builder.AppendLine(@"}).call(this);");
        }
    }
}
