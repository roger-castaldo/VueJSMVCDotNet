using VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators
{
    internal class HeaderGenerator : IJSGenerator
    {
        void IJSGenerator.GeneratorJS(WrappedStringBuilder builder, ModelType modelType, string baseURL, ILogger? log)
            => builder.AppendLine(Constants.HOST_URL_CONSTRUCTOR);
    }
}
