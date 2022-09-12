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
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType,string modelNamespace, string urlBase)
        {
            builder.AppendLine(string.Format(@"     var _{1} = function(data){{
            var ret=null;
            if (data!=null){{
                ret = {0}.{1}.{2}();
                ret.{3}(data);
            }}
            return ret;
        }};", new object[]{
                                                 modelNamespace,
                                                 modelType.Name,
                                                 Constants.CREATE_INSTANCE_FUNCTION_NAME,
                                                 Constants.PARSE_FUNCTION_NAME
            }));
            List<Type> types = new List<Type>();
            _RecurLocateLinkedTypes(ref types,modelType);
            types.Remove(modelType);
            foreach (Type type in types)
            {
                Logger.Trace("Appending Parser Call for Linked Type[{0}] for Model Definition[{1}]", new object[]
                {
                    type.FullName,
                    modelType.FullName
                });
                builder.AppendLine(string.Format(@"     var _{0} = function(data){{
            var ret=null;
            if (data!=null){{
                if ({0}.{1}!=undefined){{
                    ret = {0}.{1}.{2}();
                    ret.{2}(data);
                }}
                else {{
                    ret = {{}};
                    Object.defineProperty(ret,'id',{{get:function(){{return data.id}}}});", new object[] {
                    (((ModelJSFilePath)type.GetCustomAttributes(typeof(ModelJSFilePath), false)[0]).ModelNamespace==null ? modelNamespace : ((ModelJSFilePath)type.GetCustomAttributes(typeof(ModelJSFilePath), false)[0]).ModelNamespace),
                    type.Name, 
                    Constants.CREATE_INSTANCE_FUNCTION_NAME, 
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
                            builder.AppendLine(string.Format(@"      if (data.{2}!=null){{
                var tmp = [];
                for(var x=0;x<data.{2}.length;x++){{
                    if ({0}.{1}!=undefined){{
                        tmp.push(new {0}.{1}.{3}());
                        tmp[x].{4}(data.{2}[x]);
                    }}else{{
                        tmp[x]=_{1}(data.{2}[x]);
                    }}
                }}
                Object.defineProperty(ret,'{2}',{{get:function(){{return tmp;}}}});
            }}else{{
                Object.defineProperty(ret,'{2}',{{get:function(){{return null;}}}});
            }}", new object[]{
                                (((ModelJSFilePath)t.GetCustomAttributes(typeof(ModelJSFilePath), false)[0]).ModelNamespace==null ? modelNamespace : ((ModelJSFilePath)t.GetCustomAttributes(typeof(ModelJSFilePath), false)[0]).ModelNamespace),
                                t.Name,
                                pi.Name,
                                Constants.CREATE_INSTANCE_FUNCTION_NAME,
                                Constants.PARSE_FUNCTION_NAME,
                            }));
                        }
                        else
                        {
                            builder.AppendLine(string.Format(@"      if (data.{2}!=null){{
                if ({0}.{1}!=undefined){{
                    var tmp = {0}.{1}.{3}();
                    tmp.{4}(data.{2});
                    Object.defineProperty(ret,'{2}',{{get:function(){{return tmp;}}}});
                }}else{{
                    var tmp = _{1}(data.{2});
                    Object.defineProperty(ret,'{3}',{{get:function(){{return tmp;}}}});
                }}
            }}else{{
                Object.defineProperty(ret,'{2}',{{get:function(){{return null;}}}});
            }}", new object[]{
                                (((ModelJSFilePath)t.GetCustomAttributes(typeof(ModelJSFilePath), false)[0]).ModelNamespace==null ? modelNamespace : ((ModelJSFilePath)t.GetCustomAttributes(typeof(ModelJSFilePath), false)[0]).ModelNamespace),
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
