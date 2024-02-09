using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class JSONGenerator : IJSGenerator
    {
        public void GeneratorJS(WrappedStringBuilder builder, SModelType modelType, string urlBase, ILogger log)
        {
            log?.LogTrace("Generating toJSON method for {}", modelType.Type.FullName);
            builder.AppendLine(@$"     {Constants.TO_JSON_VARIABLE}(){{
        let attrs={{}};
        let prop=null;");
            modelType.Properties
                .Where(p => p.CanWrite)
                .ForEach(p =>
                {
                    ExtractPropertyType(p.PropertyType,out bool array,out Type propType);
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
                });
            builder.AppendLine(@$"     if (this.{Constants.INITIAL_DATA_KEY}!==undefined && this.{Constants.INITIAL_DATA_KEY}!==null){{
            Object.keys(this.{Constants.INITIAL_DATA_KEY}).filter((prop)=>isEqual(this.{Constants.INITIAL_DATA_KEY}[prop],attrs[prop])).forEach((prop)=>delete attrs[prop]);
        }}
        return _stripBigInt(cloneData(attrs));
    }}");
        }
    }
}
