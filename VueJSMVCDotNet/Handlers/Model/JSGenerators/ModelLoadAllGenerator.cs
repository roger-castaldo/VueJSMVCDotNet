using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ModelLoadAllGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, SModelType modelType, string urlBase, ILogger log)
        {
            foreach (MethodInfo mi in modelType.Type.GetMethods(Constants.LOAD_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelLoadAllMethod), false).Length > 0)
                {
                    log?.LogTrace("Adding Load All Method for Model Definition[{}]", modelType.Type.FullName);
                    builder.AppendLine(@$"     static LoadAll(){{
                            return new ModelList(
                                function(){{ return new {modelType.Type.Name}(); }},
                                {modelType.Type.Name}.#baseURL,
                                false,
                                true,
                                undefined
                            );
                        }}");
                    break;
                }
            }
        }
    }
}
