﻿using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ModelLoadGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, SModelType modelType, string urlBase, ILogger log)
        {
            log?.LogTrace("Appending Model Load method for Model Definition[{}]", modelType.Type.FullName);
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
