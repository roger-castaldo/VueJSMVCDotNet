﻿using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class StaticMethodGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType)
        {
            string urlRoot = "";
            foreach (ModelRoute mr in modelType.GetCustomAttributes(typeof(ModelRoute), false))
            {
                urlRoot = mr.Path;
                break;
            }
            if (urlRoot == "")
            {
                foreach (ModelRoute mr in modelType.GetCustomAttributes(typeof(ModelRoute), false))
                {
                    urlRoot = mr.Path;
                    break;
                }
            }
            foreach (MethodInfo mi in modelType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                {
                    bool allowNull = ((ExposedMethod)mi.GetCustomAttributes(typeof(ExposedMethod), false)[0]).AllowNullResponse;
                    builder.AppendFormat("App.Models.{0}=extend(App.Models.{0},{{{1}:function(",new object[] { modelType.Name, mi.Name });
                    ParameterInfo[] pars = Utility.ExtractStrippedParameters(mi);
                    for (int x = 0; x < pars.Length; x++)
                        builder.Append(pars[x].Name + (x + 1 == pars.Length ? "" : ","));
                    builder.AppendLine(@"){
                        var function_data = {};");
                    foreach (ParameterInfo par in pars)
                    {
                        Type propType = par.ParameterType;
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
                            if (array)
                            {
                                builder.AppendLine(string.Format(@"function_data.{0}=[];
for(var x=0;x<{0}.length;x++){{
    function_data.{0}.push({{id:{0}[x].id}});
}}", par.Name));
                            }
                            else
                                builder.AppendLine(string.Format("function_data.{0} = {{ id: {0}.id }};", par.Name));
                        }
                        else
                            builder.AppendLine(string.Format("function_data.{0} = {0};", par.Name));
                    }
                    builder.AppendLine(string.Format(@"             return new Promise((resolve,reject)=>{{
                    ajax(
                    {{
                        url:'{0}/{1}',
                        type:'SMETHOD',
                        useJSON:{2},
                        data:function_data
                    }}).then(response=>{{
                        {3}", new object[]{
                        urlRoot,
                        mi.Name,
                        (mi.GetCustomAttributes(typeof(UseFormData),false).Length==0).ToString().ToLower(),
                        (mi.ReturnType == typeof(void) ? "" : @"var ret=response.json();
                    if (ret!=undefined||ret==null)
                        response = ret;")
                    }));
                    if (mi.ReturnType != typeof(void))
                    {
                        Type propType = mi.ReturnType;
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
                        builder.AppendLine("if (response==null){");
                        if (!allowNull)
                            builder.AppendLine("reject(\"A null response was returned by the server which is invalid.\");");
                        else
                            builder.AppendLine("resolve(response);");
                        builder.AppendLine("}else{");
                        if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)))
                        {
                            if (array)
                            {
                                builder.AppendLine(string.Format(@"         ret=[];
            for (var x=0;x<response.length;x++){{
                ret.push(_{0}(response[x]));
            }}
            response = ret;", new object[]{
                                propType.Name
                                    }));
                            }
                            else
                            {
                                builder.AppendLine(string.Format(@"             ret = _{0}(response);
            response=ret;", new object[]{
                  propType.Name
                      }));
                            }
                        }
                        builder.AppendLine(@"           resolve(response);
        }");
                    }else
                        builder.AppendLine("           resolve();");
                    builder.AppendLine(@"},
                    response=>{
                        reject(response);
                    });
    });
}});");
                }
            }
        }
    }
}
