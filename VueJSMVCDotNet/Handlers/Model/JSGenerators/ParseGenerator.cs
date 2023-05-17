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
        public void GeneratorJS(ref WrappedStringBuilder builder, sModelType modelType, string urlBase)
        {
            Logger.Trace("Appending Parse method for Model Definition[{0}]", new object[] { modelType.Type.FullName });
            builder.AppendLine(string.Format(@"         {0}(jdata){{
        if (jdata==null) {{
            throw 'Unable to parse null result for a model';
        }}
        if (isString(jdata)){{
            jdata=JSON.parse(jdata);
        }}", Constants.PARSE_FUNCTION_NAME));
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
                        builder.AppendLine(string.Format(@"      if (jdata.{0}!=null){{
                let tmp = [];
                for(let x=0;x<jdata.{0}.length;x++){{
                    tmp.push(_{1}(jdata.{0}[x]));
                }}
                jdata.{0}=tmp;
            }}", pi.Name,t.Name));
                    }
                    else
                    {
                        builder.AppendLine(string.Format(@"      if (jdata.{0}!=null){{
                jdata.{0}=_{1}(jdata.{0});
            }}", pi.Name, t.Name));
                    }
                }
            }
            builder.AppendLine(string.Format("      this.{0} = jdata;", new object[] { Constants.INITIAL_DATA_KEY }));
            foreach (PropertyInfo pi in modelType.Properties)
                builder.AppendLine(string.Format("    if (jdata.{0}!==undefined){{ this.#{0}=checkProperty('{0}','{1}',(jdata.{0}===null ? null : (Array.isArray(jdata.{0}) ? jdata.{0}.slice() : jdata.{0})),'{2}'); }}", new object[]{
                    pi.Name,
                    Utility.GetTypeString(pi.PropertyType,pi.GetCustomAttribute(typeof(NotNullProperty),false)!=null),
                    Utility.GetEnumList(pi.PropertyType)
                }));
            builder.AppendLine(string.Format(@"           this.#events.trigger('{0}',this);
        return this;
        }}",new object[] {Constants.Events.MODEL_PARSED}));
        }
    }
}
