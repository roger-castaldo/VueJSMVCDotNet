using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators
{
    internal class ModelLoadAllGenerator : IJSGenerator
    {
        void IJSGenerator.GeneratorJS(StringBuilder builder, ModelType modelType, string baseURL, ILogger? log)
        {
            var mi = Array.Find(modelType.HandlerType.GetMethods(Constants.METHOD_FLAGS), mi => mi.GetCustomAttribute<ModelLoadAllMethodAttribute>(false)!=null);
            if (mi!=null)
            {
                log?.LogTrace("Adding Load All Method for Model Definition[{TypeName}]", modelType.Type.FullName);
                builder.AppendLine(@$"     static LoadAll(){{
                            return new ModelList(
                                function(){{ return new {modelType.Type.Name}(); }},
                                {modelType.Type.Name}.#baseURL,
                                false,
                                true,
                                undefined
                            );
                        }}");
            }
        }

    }
}
