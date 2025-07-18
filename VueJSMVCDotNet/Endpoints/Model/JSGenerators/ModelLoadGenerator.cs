using VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators
{
    internal class ModelLoadGenerator : IJSGenerator
    {
        void IJSGenerator.GeneratorJS(StringBuilder builder, ModelType modelType, string baseURL, ILogger? log)
        {
            log?.LogTrace("Appending Model Load method for Model Definition[{TypeName}]", modelType.Type.FullName);
            builder.AppendLine(@$"     static Load(id,callback){{
        let ret = new {modelType.Type.Name}();
        ret.{Constants.PARSE_FUNCTION_NAME}({{id:id}});
        if (callback!=undefined){{
            ret.reload().then(
                model=>{{callback(model);}},
                errored=>{{callback(null);}}
            );
        }}else{{
            ret.reload();
        }}
        return ret;
    }}");
        }
    }
}
