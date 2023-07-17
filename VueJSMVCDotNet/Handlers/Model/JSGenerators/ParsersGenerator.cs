
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ParsersGenerator : IBasicJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, string urlBase, SModelType[] models, ILogger log)
        {
            List<SModelType> types = new();
            foreach (SModelType modelType in models)
            {
                builder.AppendLine(@$"     const _{modelType.Type.Name} = function(data){{
            let ret=null;
            if (data!=null){{
                ret = new {modelType.Type.Name}();
                ret.{Constants.PARSE_FUNCTION_NAME}(data);
            }}
            return ret;
        }};");
                RecurLocateLinkedTypes(ref types, modelType);
            }
            types.RemoveAll(t => models.Contains(t));
            foreach (var type in types.Where(t=>t.Type.GetCustomAttributes().Any(att=>att is ModelJSFilePath)))
                builder.AppendLine($"        import {{ {type.Type.Name} }} from '{((ModelJSFilePath)type.Type.GetCustomAttributes(typeof(ModelJSFilePath), false)[0]).Path}';");

            foreach (SModelType type in types)
            {
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
                    foreach (PropertyInfo pi in type.Properties)
                    {
                        Type t = pi.PropertyType;
                        if (t.IsArray)
                            t = t.GetElementType();
                        else if (t.IsGenericType)
                            t = t.GetGenericArguments()[0];
                        if (new List<Type>(t.GetInterfaces()).Contains(typeof(IModel)))
                        {
                            if (Utility.IsArrayType(pi.PropertyType))
                            {
                                builder.AppendLine(@$"      if (data.{pi.Name}!=null){{
                let tmp = [];
                for(let x=0;x<data.{pi.Name}.length;x++){{");
                                if (t.GetCustomAttributes(typeof(ModelJSFilePath), false).Length>0)
                                    builder.AppendLine(@$"                 tmp.push(new {t.Name}());
                    tmp[x].{Constants.PARSE_FUNCTION_NAME}(data.{pi.Name}[x]);");
                                else
                                    builder.AppendLine($"                    tmp.push(_{t.Name}(data.{pi.Name}[x]);");
                                builder.AppendLine(@$"               }}
                ret.{pi.Name}=tmp;
            }}else{{
                ret.{pi.Name}=null;
            }}");
                            }
                            else
                            {
                                builder.AppendLine(@$"          if (data.{pi.Name}!=null){{
                {(t.GetCustomAttributes(typeof(ModelJSFilePath), false).Length==0 ? $"ret.{pi.Name} = data.{pi.Name};"
                : $@"ret.{pi.Name} = new {t.Name}();
                ret.{pi.Name}.{Constants.PARSE_FUNCTION_NAME}(data.{pi.Name});"
                )}
            }}else{{
                ret.{pi.Name}=null;
            }}");
                            }
                        }
                        else
                            builder.AppendLine($"          ret.{pi.Name} = data.{pi.Name};");
                    }
                    builder.AppendLine(@"            }
            return ret;
        };");
                }
            }
        }

        private void RecurLocateLinkedTypes(ref List<SModelType> types,SModelType modelType)
        {
            if (!types.Contains(modelType))
            {
                types.Add(modelType);
                foreach (SModelType linked in modelType.LinkedTypes)
                    RecurLocateLinkedTypes(ref types, linked);
            }
        }
    }
}
