using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class ParseGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType, string modelNamespace, string urlBase)
        {
            Logger.Trace("Appending Parse method for Model Definition[{0}]", new object[] { modelType.FullName });
            builder.AppendLine(string.Format(@"         methods = extend(methods,{{{0}:function(jdata){{
        if (jdata==null) {{
            throw 'Unable to parse null result for a model';
        }}
        if (isString(jdata)){{
            jdata=JSON.parse(jdata);
        }}", Constants.PARSE_FUNCTION_NAME));
            List<PropertyInfo> props = Utility.GetModelProperties(modelType);
            foreach (PropertyInfo pi in props)
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
                var tmp = [];
                for(var x=0;x<jdata.{0}.length;x++){{
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
            builder.AppendLine(string.Format("      setMap(this,{{ {0}:jdata }});", new object[] { Constants.INITIAL_DATA_KEY }));
            foreach (PropertyInfo pi in props)
                builder.AppendLine(string.Format("    this.{0}=(jdata.{0}==null||jdata.{0}==undefined ? (data._definition.{0}==undefined ? jdata.{0} : data._definition.{0}.initial) : (Array.isArray(jdata.{0}) ? jdata.{0}.slice() : jdata.{0}));", pi.Name));
            builder.AppendLine(@"        if (this.$emit != undefined) { this.$emit('parsed',this); }
        return this;
        }});");
        }
    }
}
