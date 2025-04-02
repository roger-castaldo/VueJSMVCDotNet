using VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Extensions;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators
{
    internal class ParsersGenerator : IBaseJSGenerator
    {
        void IBaseJSGenerator.GeneratorJS(WrappedStringBuilder builder, ModelType modelType, string baseURL, bool useModuleExtension, bool isMin, ILogger? log)
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
                if (!string.IsNullOrWhiteSpace(pair.Item2))
                {
                    builder.AppendLine(@$"     const _{pair.Item1.Name} = function(data){{
            let ret=null;
            if (data!=null){{
                ret = new {pair.Item1.Name}();
                ret.{Constants.PARSE_FUNCTION_NAME}(data);
            }}
            return ret;
        }};");
                }
                else
                {
                    builder.AppendLine(@$"     const _{pair.Item1.Name} = function(data){{
            let ret=null;
            if (data!=null){{
                ret = {{}};
                Object.defineProperty(ret,'id',{{get:function(){{return data.id;}}}});");
                    ModelType.ExtractProperties(pair.Item1).ForEach(pi =>
                    {
                        var t = Utility.ExtractUnderlyingType(pi.PropertyType, out var array, out _, out _);
                        if (new List<Type>(t.GetInterfaces()).Contains(typeof(IModel)))
                        {
                            builder.Append(@$"          ret.{pi.Name} = null;
            if (data.{pi.Name}!==null){{");
                            if (array)
                                builder.AppendLine($"ret.{pi.Name} = data.{pi.Name}.map(val=>(_{t.Name} !== undefined ? _{t.Name}(val) : val);");
                            else
                                builder.AppendLine($"ret.{pi.Name} = (_{t.Name} !== undefined ? _{t.Name}(data.{pi.Name}) : data.{pi.Name});");
                            builder.AppendLine("            }");
                        }
                        else
                            builder.AppendLine($"          ret.{pi.Name} = data.{pi.Name};");
                    });
                    builder.AppendLine(@"            }
            return ret;
        };");
                }
            });
        }
    }
}
