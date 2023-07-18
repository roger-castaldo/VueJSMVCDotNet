using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ModelClassFooterGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, SModelType modelType, string urlBase, ILogger log)
        {
            builder.AppendLine("    }");
        }
    }
}
