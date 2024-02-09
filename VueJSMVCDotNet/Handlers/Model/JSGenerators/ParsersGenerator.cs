
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ParsersGenerator : IBasicJSGenerator
    {
        public void GeneratorJS(WrappedStringBuilder builder, string urlBase, IEnumerable<SModelType> models, bool useModuleExtension, ILogger log)
        {
            List<SModelType> types = new();
            models.ForEach(modelType => ParsersGenerator.RecurLocateLinkedTypes(ref types, modelType));
            
            types.RemoveAll(t => models.Contains(t));
            types.Where(t => t.Type.GetCustomAttributes().Any(att => att is ModelJSFilePath))
                .ForEach(type => builder.AppendLine($"        import {{ {type.Type.Name} }} from '{(useModuleExtension ? type.Type.GetCustomAttribute<ModelJSFilePath>().ModulePath : type.Type.GetCustomAttribute<ModelJSFilePath>().Path)}';"));

            models.ForEach(modelType => builder.AppendLine(@$"     const _{modelType.Type.Name} = function(data){{
            let ret=null;
            if (data!=null){{
                ret = new {modelType.Type.Name}();
                ret.{Constants.PARSE_FUNCTION_NAME}(data);
            }}
            return ret;
        }};"));

            types.ForEach(type => {
                log?.LogTrace("Appending Parser Call for Linked Type[{}]", type.Type.FullName);
                if (type.Type.GetCustomAttributes(typeof(ModelJSFilePath), false).Length>0)
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
                        JSHandler.ExtractPropertyType(pi.PropertyType, out bool array, out Type t);
                        if (new List<Type>(t.GetInterfaces()).Contains(typeof(IModel)))
                        {
                            builder.Append(@$"          ret.{pi.Name} = null;
            if (data.{pi.Name}!==null){{");
                            if (array)
                                builder.AppendLine(@$"ret.{pi.Name} = data.{pi.Name}.map(val=>{{ {(t.GetCustomAttributes(typeof(ModelJSFilePath), false).Length>0 
                ? $@"let result = new {t.Name}();
                    result.{Constants.PARSE_FUNCTION_NAME}(data.{pi.Name}[x]);
                    return result;"
                : $"return _{t.Name}(data.{pi.Name}[x]);")}
}});");
                            else
                                builder.AppendLine(@$"                {(t.GetCustomAttributes(typeof(ModelJSFilePath), false).Length==0 ? $"ret.{pi.Name} = data.{pi.Name};"
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

        private static void RecurLocateLinkedTypes(ref List<SModelType> types,SModelType modelType)
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
