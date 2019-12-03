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
        public void GeneratorJS(ref WrappedStringBuilder builder, bool minimize, Type modelType)
        {
            builder.AppendLine(string.Format("    var {0}={{}};",Constants.PARSERS_VARIABLE));
            List<Type> types = new List<Type>();
            types.AddRange(_RecurLocateLinkedTypes(modelType));
            foreach (Type t in types)
                _AppendParser(t, ref builder);
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

        private void _AppendParser(Type modelType, ref WrappedStringBuilder builder)
        {
            builder.AppendLine(string.Format(@"    {1}['{0}']=function(data,model){{
        if (isString(data)){{
            data=JSON.parse(data);
        }}
        if (data==null) {{
            return null;
        }}
        var constructorMissing = model==undefined;
        if (!constructorMissing){{
            model.{2}=function(){{ return data; }};
            model.id=function(){{ return data.id; }};", new object[] { modelType.Name, Constants.PARSERS_VARIABLE, Constants.INITIAL_DATA_KEY }));
            List<PropertyInfo> props = Utility.GetModelProperties(modelType);
            foreach (PropertyInfo pi in props)
            {
                if (pi.CanWrite)
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
                        tmp.push(new App.Models.{0}());
                        tmp[x]={2}['{0}'](data.{1}[x],tmp[x]);
                    }}else{{
                        tmp.push({2}['{0}'](data.{1}[x]));
                    }}
                }}
                model.{1}=tmp;
            }}else{{
                model.{1}=null;
            }}", new object[]{
                                t.Name,
                                pi.Name,
                                Constants.PARSERS_VARIABLE
                            }));
                        }
                        else
                        {
                            builder.AppendLine(string.Format(@"         if (App.Models.{0}!=undefined) {{
                var tmp = new App.Models.{0}();
                model.{1}={2}['{0}'](data.{1},tmp);
            }} else {{
                model.{1}={2}['{0}'](data.{1});
            }}", new object[]{
                                t.Name,
                                pi.Name,
                                Constants.PARSERS_VARIABLE
                            }));
                        }
                    }
                    else if (t == typeof(DateTime) || t == typeof(DateTime?))
                    {
                        if (Utility.IsArrayType(pi.PropertyType))
                        {
                            builder.AppendLine(string.Format(@"           if (data.{0}!=null){{
                var tmp = [];
                for(var x=0;x<data.{0}.length;x++){{
                    tmp.push(new Date(data.{0}[x]));
                }}
                model.{0}=tmp;
            }}else{{
                model.{0}=null;
            }}", new object[]{
                                pi.Name
                            }));
                        }
                        else
                        {
                            builder.AppendLine(string.Format("          model.{0}=(data.{0}==null ? null : new Date(data.{0}));", new object[]
                            {
                                pi.Name
                            }));
                        }
                    }
                    else
                    {
                        if (Utility.IsArrayType(pi.PropertyType))
                        {
                            builder.AppendLine(string.Format(@"           if (data.{0}!=null){{
                var tmp = [];
                for(var x=0;x<data.{0}.length;x++){{
                    tmp.push(data.{0}[x]);
                }}
                model.{0}=tmp;
            }}else{{
                model.{0}=null;
            }}", new object[]{
                                pi.Name
                            }));
                        }
                        else
                        {
                            builder.AppendLine(string.Format("          model.{0}=data.{0};", new object[]
                            {
                                pi.Name
                            }));
                        }
                    }
                }
            }
            builder.AppendLine("        }else{");
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
                        tmp.push(new App.Models.{0}());
                        tmp[x]={2}['{0}'](data.{1}[x],tmp[x]);
                    }}else{{
                        tmp.push({2}['{0}'](data.{1}[x]));
                    }}
                }}
                data.{1}=tmp;
            }}", new object[]{
                                t.Name,
                                pi.Name,
                                Constants.PARSERS_VARIABLE
                            }));
                    }
                    else
                    {
                        builder.AppendLine(string.Format(@"         if (App.Models.{0}!=undefined) {{
                var tmp = new App.Models.{0}();
                data.{1}={2}['{0}'](data.{1},tmp);
            }} else {{
                data.{1}={2}['{0}'](data.{1});
            }}", new object[]{
                                t.Name,
                                pi.Name,
                                Constants.PARSERS_VARIABLE
                            }));
                    }
                }
                else if (t == typeof(DateTime) || t == typeof(DateTime?))
                {
                    builder.AppendLine(string.Format("          data.{0}=(data.{0}==null ? null : new Date(data.{0}));", new object[]
                    {
                            pi.Name
                    }));
                }
            }
            builder.AppendLine(string.Format(@"var tmp = Vue.extend({{
                methods:{{
                    {0}:function(){{ return data; }},
                    id:function(){{ return data.id; }}
                }},
                computed:{{",Constants.INITIAL_DATA_KEY));
            for(int x=0;x<props.Count;x++)
            {
                builder.AppendLine(string.Format(@"                  {0}:{{
                        get:function(){{
                            return this.{1}().{0};
                        }}
                    }}{2}",new object[] { props[x].Name, Constants.INITIAL_DATA_KEY,(x+1==props.Count ? "" : ",") }));
            }
            builder.AppendLine(@"           }
                });
                model = new tmp();
            }
            model.$emit('parsed',model);
            return model;
        };");

        }
    }
}
