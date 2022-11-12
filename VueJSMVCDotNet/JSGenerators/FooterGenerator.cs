using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.JSHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class FooterGenerator : IBasicJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, string urlBase, sModelType[] models)
        {
            builder.Append("export {");
            foreach (sModelType type in models)
                builder.AppendFormat("{0},", type.Type.Name);
            builder.Length=builder.Length-1;
            builder.AppendLine("}");
        }
    }
}
