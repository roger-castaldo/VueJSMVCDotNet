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
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType)
        {
            builder.AppendLine(string.Format(@"         methods = extend(methods,{{{0}:function(data){{
        if (data==null) {{
            throw 'Unable to parse null result for a model';
        }}
        if (isString(data)){{
            data=JSON.parse(data);
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
                        builder.AppendLine(string.Format(@"      if (data.{0}!=null){{
                var tmp = [];
                for(var x=0;x<data.{0}.length;x++){{
                    tmp.push(_{1}(data.{0}[x]));
                }}
                data.{0}=tmp;
            }}", pi.Name,t.Name));
                    }
                    else
                    {
                        builder.AppendLine(string.Format(@"      if (data.{0}!=null){{
                data.{0}=_{1}(data.{0});
            }}", pi.Name, t.Name));
                    }
                }
            }
            builder.AppendLine(string.Format(@"     Object.defineProperty(this,'{2}',{{get:function(){{ return data; }},configurable: true}});
            Object.defineProperty(this,'id',{{get:function(){{ return data.id; }},configurable: true}});
            for(var propName in data){{
                if (Object.getOwnPropertyDescriptor(this,propName)!=undefined){{
                    if (Object.getOwnPropertyDescriptor(this,propName).set!=undefined || Object.getOwnPropertyDescriptor(this,propName).writable){{
                        this[propName]=data[propName];
                    }}
                }}
            }}
        if (this.$emit != undefined) {{ this.$emit('parsed',this); }}
        return this;
        }}}});", new object[] { modelType.Name, Constants.CREATE_INSTANCE_FUNCTION_NAME,Constants.INITIAL_DATA_KEY }));
        }
    }
}
