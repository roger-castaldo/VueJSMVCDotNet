using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Extensions;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators
{
    internal class JSONGenerator : IJSGenerator
    {
        void IJSGenerator.GeneratorJS(WrappedStringBuilder builder, ModelType modelType, string baseURL, ILogger? log)
        {
            log?.LogTrace("Generating toJSON method for {TypeName}", modelType.Type.FullName);
            builder.AppendLine(@$"     {Constants.TO_JSON_VARIABLE}(){{
        let attrs={{}};
        let prop=null;");
            modelType.Properties
                .Where(p => p.CanWrite)
                .ForEach(p =>
                {
                    var propType = Utility.ExtractUnderlyingType(p.PropertyType, out var array, out _, out _);
                    if (p.GetCustomAttribute<ReadOnlyModelPropertyAttribute>(false)!=null)
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
