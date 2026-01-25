using VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Extensions;

namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators
{
    internal class ParsersGenerator : IBaseJSGenerator
    {
        void IBaseJSGenerator.GeneratorJS(StringBuilder builder, ModelType modelType, string baseURL, bool useModuleExtension, bool isMin, ILogger? log)
        {
            var ext = (useModuleExtension, isMin) switch
            {
                (true, _) => "mjs",
                (false, true) => "min.js",
                _ => "js"
            };
            modelType.LinkedTypes
                .Where(tuple => !string.IsNullOrWhiteSpace(tuple.Item2))
                .ForEach(tuple => builder.AppendLine($"        import {{ {tuple.Item1.Name} }} from '{tuple.Item2}.{ext}';"));

            builder.AppendLine(@$"     const _{modelType.Type.Name} = function(data){{
            let ret=null;
            if (data!=null){{
                ret = new {modelType.Type.Name}();
                ret.{Constants.PARSE_FUNCTION_NAME}(data);
            }}
            return ret;
        }};");

            modelType.LinkedTypes.ForEach(pair =>
            {
                log?.LogTrace("Appending Parser Call for Linked Type[{TypeName}]", pair.Item1.FullName);
                builder.AppendLine(@$"     const _{pair.Item1.Name} = function(data){{
            let ret=null;
            if (data!=null){{
                ret = new {pair.Item1.Name}();
                ret.{Constants.PARSE_FUNCTION_NAME}(data);
            }}
            return ret;
        }};");
            });
        }
    }
}
