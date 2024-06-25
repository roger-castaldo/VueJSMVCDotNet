
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ParsersGenerator : IBasicJSGenerator
    {
        public void GeneratorJS(WrappedStringBuilder builder, string? urlBase, IEnumerable<SModelType> models, bool useModuleExtension, ILogger? log)
        {
            List<SModelType> types = [];
            models.ForEach(modelType => ParsersGenerator.RecurLocateLinkedTypes(ref types, modelType));

            types.RemoveAll(t => models.Contains(t));
            types.Select(t => new { type = t, Path = t.Type.GetCustomAttribute<ModelJSFilePathAttribute>() })
                .Where(gt => gt.Path!=null)
                .ForEach(gt => builder.AppendLine($"        import {{ {gt.type.Type.Name} }} from '{(useModuleExtension ? gt.Path!.ModulePath : gt.Path!.Path)}';"));

            models.ForEach(modelType => builder.AppendLine(@$"     const _{modelType.Type.Name} = function(data){{
            let ret=null;
            if (data!=null){{
                ret = new {modelType.Type.Name}();
                ret.{Constants.PARSE_FUNCTION_NAME}(data);
            }}
            return ret;
        }};"));

            types.ForEach(type =>
            {
                log?.LogTrace("Appending Parser Call for Linked Type[{TypeName}]", type.Type.FullName);
                if (type.Type.GetCustomAttributes(typeof(ModelJSFilePathAttribute), false).Length>0)
                {
                    builder.AppendLine(@$"     const _{type.Type.Name} = function(data){{
            let ret=null;
            if (data!=null){{
                ret = new {type.Type.Name}();
                ret.{Constants.PARSE_FUNCTION_NAME}(data);
            }}
            return ret;
        }};");
                }
                else
                {
                    builder.AppendLine(@$"     const _{type.Type.Name} = function(data){{
            let ret=null;
            if (data!=null){{
                ret = {{}};
                Object.defineProperty(ret,'id',{{get:function(){{return data.id;}}}});");
                    type.Properties.ForEach(pi =>
                    {
                        var t = Utility.ExtractUnderlyingType(pi.PropertyType, out var array, out _, out _);
                        if (new List<Type>(t.GetInterfaces()).Contains(typeof(IModel)))
                        {
                            builder.Append(@$"          ret.{pi.Name} = null;
            if (data.{pi.Name}!==null){{");
                            if (array)
                                builder.AppendLine(@$"ret.{pi.Name} = data.{pi.Name}.map(val=>{{ {(t.GetCustomAttribute<ModelJSFilePathAttribute>(false)!=null
                ? $@"let result = new {t.Name}();
                    result.{Constants.PARSE_FUNCTION_NAME}(data.{pi.Name}[x]);
                    return result;"
                : $"return _{t.Name}(data.{pi.Name}[x]);")}
}});");
                            else
                                builder.AppendLine(@$"                {(t.GetCustomAttribute<ModelJSFilePathAttribute>(false)!=null ? $"ret.{pi.Name} = data.{pi.Name};"
                : $@"ret.{pi.Name} = new {t.Name}();
                ret.{pi.Name}.{Constants.PARSE_FUNCTION_NAME}(data.{pi.Name});"
                )}");
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

        private static void RecurLocateLinkedTypes(ref List<SModelType> types, SModelType modelType)
        {
            if (!types.Contains(modelType))
            {
                types.Add(modelType);
                foreach (SModelType linked in modelType.LinkedTypes)
                    ParsersGenerator.RecurLocateLinkedTypes(ref types, linked);
            }
        }
    }
}
