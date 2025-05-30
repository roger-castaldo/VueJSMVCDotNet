using VueJSMVCDotNet.Attributes.Models;
using VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Extensions;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators
{
    internal class ParseGenerator : IJSGenerator
    {
        void IJSGenerator.GeneratorJS(WrappedStringBuilder builder, ModelType modelType, string baseURL, ILogger? log)
        {
            log?.LogTrace("Appending Parse method for Model Definition[{TypeName}]", modelType.Type.FullName);
            builder.AppendLine(@$"         {Constants.PARSE_FUNCTION_NAME}(jdata){{
        if (jdata==null) {{
            throw 'Unable to parse null result for a model';
        }}
        if (isString(jdata)){{
            jdata=JSON.parse(jdata);
        }}");
            modelType.Properties.ForEach(pi =>
            {
                (var t, _, _, _, _) = Utility.ExtractUnderlyingType(pi.PropertyType);
                if (new List<Type>(t.GetInterfaces()).Contains(typeof(IModel)))
                {
                    if (Utility.IsArrayType(pi.PropertyType))
                    {
                        builder.AppendLine(@$"      if (jdata.{pi.Name}!=null){{
                let tmp = [];
                for(let x=0;x<jdata.{pi.Name}.length;x++){{
                    tmp.push(_{t.Name}(jdata.{pi.Name}[x]));
                }}
                jdata.{pi.Name}=tmp;
            }}");
                    }
                    else
                    {
                        builder.AppendLine(@$"      if (jdata.{pi.Name}!=null){{
                jdata.{pi.Name}=_{t.Name}(jdata.{pi.Name});
            }}");
                    }
                }
            });
            builder.AppendLine($"      this.{Constants.INITIAL_DATA_KEY} = jdata;");
            modelType.Properties.ForEach(pi => builder.AppendLine($"    if (jdata.{pi.Name}!==undefined){{ this.#{pi.Name}=checkProperty('{pi.Name}','{Utility.GetTypeString(pi.PropertyType, pi.GetCustomAttribute(typeof(NotNullPropertyAttribute), false)!=null)}',(jdata.{pi.Name}===null ? null : (Array.isArray(jdata.{pi.Name}) ? jdata.{pi.Name}.slice() : jdata.{pi.Name})),{Utility.GetEnumList(pi.PropertyType)}); }}"));
            builder.AppendLine(@$"           this.#events.trigger('{Constants.Events.MODEL_PARSED}',this);
        return this;
        }}");
        }
    }
}
