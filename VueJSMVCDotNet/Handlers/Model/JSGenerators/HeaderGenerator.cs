using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class HeaderGenerator : IBasicJSGenerator
    {
        public void GeneratorJS(WrappedStringBuilder builder, string urlBase, IEnumerable<JSHandler.SModelType> models, bool useModuleExtension, ILogger log)
        {
            builder.AppendLine(Constants.HOST_URL_CONSTRUCTOR);
        }
    }
}
