using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class HeaderGenerator : IBasicJSGenerator
    {
        public void GeneratorJS(WrappedStringBuilder builder, string? urlBase, IEnumerable<SModelType> models, bool useModuleExtension, ILogger? log)
            => builder.AppendLine(Constants.HOST_URL_CONSTRUCTOR);
    }
}
