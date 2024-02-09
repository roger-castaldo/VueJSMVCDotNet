using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces
{
    internal interface IJSGenerator
    {
        void GeneratorJS(WrappedStringBuilder builder, SModelType modelType, string urlBase,ILogger log);
    }
}
