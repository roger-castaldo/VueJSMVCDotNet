using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class ParsersGenerator : IBasicJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, string urlBase, Type[] models)
        {
            List<Type> types = new List<Type>();
            foreach (Type modelType in models)
            {
                builder.AppendLine(string.Format(@"     const _{0} = function(data){{
            let ret=null;
            if (data!=null){{
                ret = new {0}();
                ret.{1}(data);
            }}
            return ret;
        }};", new object[]{
                                                 modelType.Name,
                                                 Constants.PARSE_FUNCTION_NAME
            }));
                _RecurLocateLinkedTypes(ref types, modelType);
            }
            foreach (Type modelType in models)
                types.Remove(modelType);
            foreach (Type type in types)
            {
                if (type.GetCustomAttributes(typeof(ModelJSFilePath), false).Length>0)
                    builder.AppendLine(string.Format("        import {{ {0} }} from '{1}';", new object[] { type.Name, ((ModelJSFilePath)type.GetCustomAttributes(typeof(ModelJSFilePath), false)[0]).Path }));
            }
            foreach (Type type in types)
            {
                Logger.Trace("Appending Parser Call for Linked Type[{0}]", new object[]
                {
                    type.FullName
                });
                if (type.GetCustomAttributes(typeof(ModelJSFilePath), false).Length>0)
                {
                    builder.AppendLine(string.Format(@"     const _{0} = function(data){{
            let ret=null;
            if (data!=null){{
                ret = new {0}();
                ret.{1}(data);
            }}
            return ret;
        }};", new object[]{
                        type.Name,
                        Constants.PARSE_FUNCTION_NAME
                    }));
                }
                else
                {
                    builder.AppendLine(string.Format(@"     const _{0} = function(data){{
            let ret=null;
            if (data!=null){{
                ret = {{}};
                Object.defineProperty(ret,'id',{{get:function(){{return data.id}}}});", new object[] {
                    type.Name,
                    Constants.PARSE_FUNCTION_NAME }));
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
                                builder.AppendLine(string.Format(@"      if (data.{0}!=null){{
                let tmp = [];
                for(let x=0;x<data.{0}.length;x++){{", new object[] { pi.Name }));
                                if (t.GetCustomAttributes(typeof(ModelJSFilePath), false).Length>0)
                                    builder.AppendLine(string.Format(@"                 tmp.push(new {0}());
                    tmp[x].{1}(data.{2}[x]);", new object[]
                                    {
                                        t.Name,
                                        Constants.PARSE_FUNCTION_NAME,
                                        pi.Name
                                    }));
                                else
                                    builder.AppendLine(string.Format("                    tmp.push(_{0}(data.{1}[x]);", new object[] { t.Name, pi.Name }));
                                builder.AppendLine(string.Format(@"               }}
                ret.{0}=tmp;
            }}else{{
                ret.{0}=null;
            }}", new object[]{
                                pi.Name
                            }));
                            }
                            else
                            {
                                builder.AppendLine(string.Format("          if (data.{0}!=null){{", new object[] { pi.Name }));
                                if (t.GetCustomAttributes(typeof(ModelJSFilePath), false).Length>0)
                                    builder.AppendLine(string.Format(@"              ret.{0} = new {1}();
                ret.{0}.{1}(data{0});", new object[] { pi.Name, t.Name }));
                                else
                                    builder.AppendLine(string.Format("              ret.{0} = data.{0}", new object[] { pi.Name }));
                                builder.AppendLine(string.Format(@"          }}else{{
                ret.{0}=null;
            }}", new object[] { pi.Name }));
                            }
                        }
                        else
                            builder.AppendLine(string.Format("          ret.{0} = data.{0};", pi.Name));
                        builder.AppendLine(@"           }
            }
            return ret;
        };");
                    }
                }
            }
        }

        private void _RecurLocateLinkedTypes(ref List<Type> types,Type modelType)
        {
            if (!types.Contains(modelType))
            {
                types.Add(modelType);
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
                            if (!types.Contains(t))
                            {
                                types.Add(t);
                                _RecurLocateLinkedTypes(ref types,t);
                            }
                        }
                    }
                }
                foreach (BindingFlags bf in new BindingFlags[] { Constants.INSTANCE_METHOD_FLAGS, Constants.STATIC_INSTANCE_METHOD_FLAGS })
                {
                    foreach (MethodInfo mi in modelType.GetMethods(bf))
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
                                if (!types.Contains(t))
                                {
                                    types.Add(t);
                                    _RecurLocateLinkedTypes(ref types, t);
                                }
                            }
                            t = ((ExposedMethod)mi.GetCustomAttributes(typeof(ExposedMethod), false)[0]).ArrayElementType;
                            if (t!=null)
                            {
                                if (new List<Type>(t.GetInterfaces()).Contains(typeof(IModel)))
                                {
                                    if (!types.Contains(t))
                                    {
                                        types.Add(t);
                                        _RecurLocateLinkedTypes(ref types, t);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
