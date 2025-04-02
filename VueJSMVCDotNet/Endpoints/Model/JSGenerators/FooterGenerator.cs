using VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators
{
    internal class FooterGenerator : IJSGenerator
    {
        void IJSGenerator.GeneratorJS(WrappedStringBuilder builder, ModelType modelType, string baseURL, ILogger? log)
            => builder.AppendLine($"export {{{modelType.Type.Name}}}");
    }
}
