using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.JSHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class ModelClassFooterGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, sModelType modelType, string urlBase)
        {
            builder.AppendLine("    }");
        }
    }
}
