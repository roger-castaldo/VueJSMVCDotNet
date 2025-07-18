
namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces
{
    internal interface IBaseJSGenerator : IGenerator
    {
        void GeneratorJS(StringBuilder builder, ModelType modelType, string baseURL, bool useModuleExtension, bool isMin, ILogger? log);
    }
}
