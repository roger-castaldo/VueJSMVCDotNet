using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ParseGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, sModelType modelType, string urlBase, ILog log)
        {
            log.Trace("Appending Parse method for Model Definition[{0}]", new object[] { modelType.Type.FullName });
            builder.AppendLine(@$"         {Constants.PARSE_FUNCTION_NAME}(jdata){{
        if (jdata==null) {{
            throw 'Unable to parse null result for a model';
        }}
        if (isString(jdata)){{
            jdata=JSON.parse(jdata);
        }}");
            foreach (PropertyInfo pi in modelType.Properties)
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
            }
            builder.AppendLine($"      this.{Constants.INITIAL_DATA_KEY} = jdata;");
            foreach (PropertyInfo pi in modelType.Properties)
                builder.AppendLine($"    if (jdata.{pi.Name}!==undefined){{ this.#{pi.Name}=checkProperty('{pi.Name}','{Utility.GetTypeString(pi.PropertyType, pi.GetCustomAttribute(typeof(NotNullProperty), false)!=null)}',(jdata.{pi.Name}===null ? null : (Array.isArray(jdata.{pi.Name}) ? jdata.{pi.Name}.slice() : jdata.{pi.Name})),'{Utility.GetEnumList(pi.PropertyType)}'); }}");
            builder.AppendLine(@$"           this.#events.trigger('{Constants.Events.MODEL_PARSED}',this);
        return this;
        }}");
        }
    }
}
