using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class ParsersGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, bool minimize, Type modelType)
        {
            builder.AppendLine(string.Format("    {0}={{}};",Constants.PARSERS_VARIABLE));
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
                if (pi.CanRead && pi.GetCustomAttributes(typeof(ReadOnlyModelProperty), true).Length == 0)
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
        }}", new object[] { modelType.Name, Constants.PARSERS_VARIABLE }));
            builder.AppendLine(string.Format(@"      model.{0}=function(){{ return data; }};
        model.id=function(){{ return data.id; }};", Constants.INITIAL_DATA_KEY));

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
                        if (pi.PropertyType.IsArray || pi.PropertyType.IsGenericType)
                        {
                            builder.AppendLine(string.Format(@"     if (data.{1}!=null){{
            var tmp = [];
            for(var x=0;x<data.{1}.length;x++){{
                tmp.push(new App.Models.{0}());
                tmp[x]={2}['{0}'](data.{1}[x],tmp[x]);
            }}
            model.{1}=tmp;
        }}", new object[]{
                                t.Name,
                                pi.Name,
                                Constants.PARSERS_VARIABLE
                            }));
                        }
                        else
                        {
                            builder.AppendLine(string.Format(@"     var tmp = new App.Models.{0}();
        model.{1}={2}['{0}'](data.{1},tmp);", new object[]{
                                t.Name,
                                pi.Name,
                                Constants.PARSERS_VARIABLE
                            }));
                        }
                    }
                    else
                    {
                        builder.AppendLine(string.Format("      model.{0}=data.{0};", new object[]
                        {
                            pi.Name
                        }));
                    }
                }
            }
            builder.AppendLine(@"           model.$emit('parsed',model);
            return model;
        };");

        }
    }
}
