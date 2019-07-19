using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class JSONGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, bool minimize, Type modelType)
        {
            builder.AppendLine(string.Format(@"   var {0} = function(model){{
        var attrs={{}};", Constants.TO_JSON_VARIABLE));
            foreach (PropertyInfo p in Utility.GetModelProperties(modelType))
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
                if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)))
                {
                    builder.AppendLine(string.Format(@"     if (model.{0}!=undefined) {{
            if (model.{0}==null) {{
                attrs.{0} = null;
            }} else {{", (!p.CanRead || p.GetCustomAttributes(typeof(ReadOnlyModelProperty), false).Length > 0 ? Constants.INITIAL_DATA_KEY + "()." : "") + p.Name));
                    if (array)
                    {
                        builder.AppendLine(string.Format(@"         for(var x=0;x<model.{0}.length;x++){{
                            attrs.{0}.push({{id:model.{0}[x].id()}});
                        }}", (!p.CanRead || p.GetCustomAttributes(typeof(ReadOnlyModelProperty), false).Length > 0 ? Constants.INITIAL_DATA_KEY + "()." : "") + p.Name));
                    }
                    else
                        builder.AppendLine(string.Format("      attrs.{0} = {{id:model.{0}.id()}};", (!p.CanRead || p.GetCustomAttributes(typeof(ReadOnlyModelProperty), false).Length > 0 ? Constants.INITIAL_DATA_KEY + "()." : "") + p.Name));
                    builder.AppendLine(@"           }
        }");
                }
                else
                    builder.AppendLine(string.Format("        attrs.{0}=model.{0};", (!p.CanRead || p.GetCustomAttributes(typeof(ReadOnlyModelProperty), false).Length > 0 ? Constants.INITIAL_DATA_KEY + "()." : "") + p.Name));
            }
            builder.AppendLine(string.Format(@"     if (model.{0}!=undefined){{
            var tmp = model.{0}();
            for(var prop in tmp){{
                if (_.isEqual(tmp[prop],attrs[prop])){{
                    delete attrs[prop];
                }}
            }}
        }}
        return attrs;
    }};",Constants.INITIAL_DATA_KEY));
        }
    }
}
