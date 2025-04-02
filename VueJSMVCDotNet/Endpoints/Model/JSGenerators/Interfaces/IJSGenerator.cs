namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces
{
    internal interface IJSGenerator : IGenerator
    {
        void GeneratorJS(WrappedStringBuilder builder, ModelType modelType, string baseURL, ILogger? log);
    }
}
