using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class FooterGenerator : IBasicJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, string urlBase, Type[] models)
        {
            builder.Append("export {");
            foreach (Type type in models)
                builder.AppendFormat("{0},", type.Name);
            builder.Length=builder.Length-1;
            builder.AppendLine("}");
        }
    }
}
