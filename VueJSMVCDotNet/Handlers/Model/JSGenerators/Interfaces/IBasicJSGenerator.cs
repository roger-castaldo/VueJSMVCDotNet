using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces
{
    internal interface IBasicJSGenerator
    {
        void GeneratorJS(ref WrappedStringBuilder builder,string urlBase, SModelType[] models, ILogger log);
    }
}
