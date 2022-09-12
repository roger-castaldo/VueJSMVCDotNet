using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class FooterGenerator : IBasicJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, string modelNamespace, string urlBase)
        {
            builder.AppendLine(@"}).call(this);");
        }
    }
}
