using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces
{
    internal interface IBasicJSGenerator
    {
        void GeneratorJS(WrappedStringBuilder builder,string urlBase, IEnumerable<SModelType> models,bool useModuleExtension, ILogger log);
    }
}
