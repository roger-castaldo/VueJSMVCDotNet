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
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType, string modelNamespace, string urlBase)
        {
            Logger.Trace("Generating toJSON method for {0}", new object[] { modelType.FullName });
            builder.AppendLine(string.Format(@"   methods = extend(methods,{{{0}:function(){{
        var attrs={{}};
        var prop=null;", Constants.TO_JSON_VARIABLE));
            foreach (PropertyInfo p in Utility.GetModelProperties(modelType))
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
                        builder.AppendLine(string.Format("            prop = (this.{1}==undefined ? (this.{0}!=undefined ? this.{0} : null) : (this.{1}.{0}!=undefined ? this.{1}.{0} : null));", p.Name,Constants.INITIAL_DATA_KEY));
                    else
                        builder.AppendLine(string.Format("            prop = (this.{0}!=undefined ? this.{0} : null);", p.Name));
                    if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)))
                    {
                        builder.AppendLine(string.Format(@"     if (prop==null) {{
                attrs.{0} = null;
            }} else {{", p.Name));
                        if (array)
                        {
                            builder.AppendLine(string.Format(@"         for(var x=0;x<prop.length;x++){{
                            attrs.{0}.push({{id:prop[x].id}});
                        }}", p.Name));
                        }
                        else
                            builder.AppendLine(string.Format("      attrs.{0} = {{id:prop.id}};", p.Name));
                        builder.AppendLine(@"           }");
                    }
                    else
                        builder.AppendLine(string.Format("        attrs.{0}=prop;",p.Name));
                }
            }
            builder.AppendLine(string.Format(@"     var idata = (getMap(this)==undefined ? undefined : getMap(this).{0});
        if (idata!=undefined){{
            for(prop in idata){{
                if (isEqual(idata[prop],attrs[prop])){{
                    delete attrs[prop];
                }}
            }}
        }}
        return _stripBigInt(cloneData(attrs));
    }}}});", Constants.INITIAL_DATA_KEY));
        }
    }
}
