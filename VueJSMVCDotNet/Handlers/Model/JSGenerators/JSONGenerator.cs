using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class JSONGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, SModelType modelType, string urlBase, ILogger log)
        {
            log?.LogTrace("Generating toJSON method for {}",  modelType.Type.FullName);
            builder.AppendLine(@$"     {Constants.TO_JSON_VARIABLE}(){{
        let attrs={{}};
        let prop=null;");
            foreach (PropertyInfo p in modelType.Properties)
            {
                if (p.CanWrite)
                {
                    Type propType = p.PropertyType;
                    bool array = false;
                    if (propType.FullName.StartsWith("System.Nullable"))
                    {
                        if (propType.IsGenericType)
                            propType = propType.GetGenericArguments()[0];
                        else
                            propType = propType.GetElementType();
                    }
                    if (propType.IsArray)
                    {
                        array = true;
                        propType = propType.GetElementType();
                    }
                    else if (propType.IsGenericType)
                    {
                        if (propType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            array = true;
                            propType = propType.GetGenericArguments()[0];
                        }
                    }
                    if (p.GetCustomAttributes(typeof(ReadOnlyModelProperty), false).Length > 0)
                        builder.AppendLine($"            prop = (this.{Constants.INITIAL_DATA_KEY}===undefined||this.#{p.Name}===null ? (this.#{p.Name}!==undefined ? this.#{p.Name} : null) : (this.{Constants.INITIAL_DATA_KEY}.{p.Name}!==undefined ? this.{Constants.INITIAL_DATA_KEY}.{p.Name} : null));");
                    else
                        builder.AppendLine($"            prop = (this.#{p.Name}!==undefined ? this.#{p.Name} : null);");
                    if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)))
                    {
                        builder.AppendLine(@$"     if (prop===null) {{
                attrs.{p.Name} = null;
            }} else {{");
                        if (array)
                        {
                            builder.AppendLine(@$"         for(let x=0;x<prop.length;x++){{
                            attrs.{p.Name}.push({{id:prop[x].id}});
                        }}");
                        }
                        else
                            builder.AppendLine($"      attrs.{p.Name} = {{id:prop.id}};");
                        builder.AppendLine("           }");
                    }
                    else
                        builder.AppendLine($"        attrs.{p.Name}=prop;");
                }
            }
            builder.AppendLine(@$"     if (this.{Constants.INITIAL_DATA_KEY}!==undefined && this.{Constants.INITIAL_DATA_KEY}!==null){{
            for(prop in this.{Constants.INITIAL_DATA_KEY}){{
                if (isEqual(this.{Constants.INITIAL_DATA_KEY}[prop],attrs[prop])){{
                    delete attrs[prop];
                }}
            }}
        }}
        return _stripBigInt(cloneData(attrs));
    }}");
        }
    }
}
