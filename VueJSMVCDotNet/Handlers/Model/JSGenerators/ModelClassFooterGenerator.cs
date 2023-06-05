using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ModelClassFooterGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, sModelType modelType, string urlBase, ILog log)
        {
            builder.AppendLine("    }");
        }
    }
}
