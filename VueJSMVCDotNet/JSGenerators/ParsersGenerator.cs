using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class ParsersGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType)
        {
            builder.AppendLine(string.Format(@"     var _{0} = function(data){{
            var ret=null;
            if (data!=null){{
                ret = App.Models.{0}.{1}();
                ret.{2}(data);
            }}
            return ret;
        }};", new object[]{
                                                 modelType.Name,
                                                 Constants.CREATE_INSTANCE_FUNCTION_NAME,
                                                 Constants.PARSE_FUNCTION_NAME
            }));
            List<Type> types = _RecurLocateLinkedTypes(modelType);
            types.Remove(modelType);
            foreach (Type type in types)
            {
                builder.AppendLine(string.Format(@"     var _{0} = function(data){{
            var ret=null;
            if (data!=null){{
                if (App.Models.{0}!=undefined){{
                    ret = App.Models.{0}.{1}();
                    ret.{2}(data);
                }}
                else {{
                    ret = {{}};
                    Object.defineProperty(ret,'id',{{get:function(){{return data.id}}}});", new object[] { type.Name, Constants.CREATE_INSTANCE_FUNCTION_NAME, Constants.PARSE_FUNCTION_NAME }));
                List<PropertyInfo> props = Utility.GetModelProperties(type);
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
                            builder.AppendLine(string.Format(@"      if (data.{1}!=null){{
                var tmp = [];
                for(var x=0;x<data.{1}.length;x++){{
                    if (App.Models.{0}!=undefined){{
                        tmp.push(new App.Models.{0}.{2}());
                        tmp[x].{3}(data.{1}[x]);
                    }}else{{
                        tmp[x]=_{0}(data.{1}[x]);
                    }}
                }}
                Object.defineProperty(ret,'{1}',{{get:function(){{return tmp;}}}});
            }}else{{
                Object.defineProperty(ret,'{1}',{{get:function(){{return null;}}}});
            }}", new object[]{
                                t.Name,
                                pi.Name,
                                Constants.CREATE_INSTANCE_FUNCTION_NAME,
                                Constants.PARSE_FUNCTION_NAME,
                            }));
                        }
                        else
                        {
                            builder.AppendLine(string.Format(@"      if (data.{1}!=null){{
                if (App.Models.{0}!=undefined){{
                    var tmp = App.Models.{0}.{2}();
                    tmp.{3}(data.{1});
                    Object.defineProperty(ret,'{1}',{{get:function(){{return tmp;}}}});
                }}else{{
                    var tmp = _{0}(data.{1});
                    Object.defineProperty(ret,'{1}',{{get:function(){{return tmp;}}}});
                }}
            }}else{{
                Object.defineProperty(ret,'{1}',{{get:function(){{return null;}}}});
            }}", new object[]{
                                t.Name,
                                pi.Name,
                                Constants.CREATE_INSTANCE_FUNCTION_NAME,
                                Constants.PARSE_FUNCTION_NAME,
                            }));
                        }
                    }
                    else
                    {
                        builder.AppendLine(string.Format("          Object.defineProperty(ret,'{0}',{{get:function(){{return data.{0};}}}});", pi.Name));
                    }
                }
                builder.AppendLine(@"           }
            }
            return ret;
        };");
            }
        }

        private List<Type> _RecurLocateLinkedTypes(Type modelType)
        {
            List<Type> ret = new List<Type>(new Type[] { modelType });
            foreach (PropertyInfo pi in Utility.GetModelProperties(modelType))
            {
                if (pi.CanRead)
                {
                    Type t = pi.PropertyType;
                    if (t.IsArray)
                        t = t.GetElementType();
                    else if (t.IsGenericType)
                        t = t.GetGenericArguments()[0];
                    if (new List<Type>(t.GetInterfaces()).Contains(typeof(IModel)))
                    {
                        if (!ret.Contains(t))
                        {
                            ret.Add(t);
                            List<Type> tmp = _RecurLocateLinkedTypes(t);
                            foreach (Type type in tmp)
                            {
                                if (!ret.Contains(type))
                                    ret.Add(type);
                            }
                        }
                    }
                }
            }
            foreach (MethodInfo mi in modelType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                {
                    Type t = mi.ReturnType;
                    if (t.IsArray)
                        t = t.GetElementType();
                    else if (t.IsGenericType)
                        t = t.GetGenericArguments()[0];
                    if (new List<Type>(t.GetInterfaces()).Contains(typeof(IModel)))
                    {
                        if (!ret.Contains(t))
                        {
                            ret.Add(t);
                            List<Type> tmp = _RecurLocateLinkedTypes(t);
                            foreach (Type type in tmp)
                            {
                                if (!ret.Contains(type))
                                    ret.Add(type);
                            }
                        }
                    }
                }
            }
            foreach (MethodInfo mi in modelType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                {
                    Type t = mi.ReturnType;
                    if (t.IsArray)
                        t = t.GetElementType();
                    else if (t.IsGenericType)
                        t = t.GetGenericArguments()[0];
                    if (new List<Type>(t.GetInterfaces()).Contains(typeof(IModel)))
                    {
                        if (!ret.Contains(t))
                        {
                            ret.Add(t);
                            List<Type> tmp = _RecurLocateLinkedTypes(t);
                            foreach (Type type in tmp)
                            {
                                if (!ret.Contains(type))
                                    ret.Add(type);
                            }
                        }
                    }
                }
            }
            return ret;
        }
    }
}
