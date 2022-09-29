using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class StaticMethodGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType,string modelNamespace, string urlBase)
        {
            string urlRoot = Utility.GetModelUrlRoot(modelType, urlBase);
            foreach (MethodInfo mi in modelType.GetMethods(Constants.STATIC_INSTANCE_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                {
                    Logger.Trace("Appending Static Exposed Method[{0}] to Model Definition[{1}]", new object[]
                    {
                        mi.Name,
                        modelType.FullName
                    });
                    ExposedMethod em = (ExposedMethod)mi.GetCustomAttributes(typeof(ExposedMethod), false)[0];
                    Type returnType = (em.ArrayElementType!=null ? Array.CreateInstance(em.ArrayElementType, 0).GetType() : mi.ReturnType);
                    bool array = false;
                    if (returnType != typeof(void))
                    {
                        if (returnType.FullName.StartsWith("System.Nullable"))
                        {
                            if (returnType.IsGenericType)
                                returnType = returnType.GetGenericArguments()[0];
                            else
                                returnType = returnType.GetElementType();
                        }
                        if (returnType.IsArray)
                        {
                            array = true;
                            returnType = returnType.GetElementType();
                        }
                        else if (returnType.IsGenericType)
                        {
                            if (returnType.GetGenericTypeDefinition() == typeof(List<>))
                            {
                                array = true;
                                returnType = returnType.GetGenericArguments()[0];
                            }
                        }
                    }
                    builder.AppendFormat("{0}.{1}=extend({0}.{1},{{{2}:function(",new object[] { modelNamespace,modelType.Name, mi.Name });
                    ParameterInfo[] pars = Utility.ExtractStrippedParameters(mi);
                    for (int x = 0; x < pars.Length; x++)
                        builder.Append(pars[x].Name + (x + 1 == pars.Length ? "" : ","));
                    builder.AppendLine(@"){
                        var function_data = {};");
                    NotNullArguement nna = (mi.GetCustomAttributes(typeof(NotNullArguement), false).Length == 0 ? null : (NotNullArguement)mi.GetCustomAttributes(typeof(NotNullArguement), false)[0]);
                    foreach (ParameterInfo par in pars)
                    {
                        Type propType = par.ParameterType;
                        bool parray = false;
                        if (propType.FullName.StartsWith("System.Nullable"))
                        {
                            if (propType.IsGenericType)
                                propType = propType.GetGenericArguments()[0];
                            else
                                propType = propType.GetElementType();
                        }
                        if (propType.IsArray)
                        {
                            parray = true;
                            propType = propType.GetElementType();
                        }
                        else if (propType.IsGenericType)
                        {
                            if (propType.GetGenericTypeDefinition() == typeof(List<>))
                            {
                                parray = true;
                                propType = propType.GetGenericArguments()[0];
                            }
                        }
                        if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)))
                        {
                            if (parray)
                            {
                                builder.AppendLine(string.Format(@"function_data.{0}=[];
for(var x=0;x<{0}.length;x++){{
    function_data.{0}.push({{id:{0}[x].id}});
}}", par.Name));
                            }
                            else
                                builder.AppendLine(string.Format("function_data.{0} = _checkProperty('{0}','{1}',{0},{2});", new object[]
                                {
                                    par.Name,
                                    Utility.GetTypeString(par.ParameterType,(nna==null ? false : !nna.IsParameterNullable(par))),
                                    Utility.GetEnumList(par.ParameterType)
                                }));
                        }
                        else
                            builder.AppendLine(string.Format("function_data.{0} = _checkProperty('{0}','{1}',{0},{2});", new object[]
                            {
                                par.Name,
                                Utility.GetTypeString(par.ParameterType,(nna==null ? false : !nna.IsParameterNullable(par))),
                                Utility.GetEnumList(par.ParameterType)
                            }));
                    }
                    builder.AppendLine(string.Format(@"             return new Promise((resolve,reject)=>{{
                    ajax(
                    {{
                        url:'{0}/{1}',
                        type:'SMETHOD',
                        useJSON:{2},
                        data:function_data{4}
                    }}).then(response=>{{
                        {3}", new object[]{
                        urlRoot,
                        mi.Name,
                        (mi.GetCustomAttributes(typeof(UseFormData),false).Length==0).ToString().ToLower(),
                        (returnType == typeof(void) ? "" : @"var ret=response.json();
                    if (ret!=undefined||ret==null)
                        response = ret;"),
                        (em.IsSlow ? ",isSlow:true,isArray:"+array.ToString().ToLower() : "")
                    }));
                    if (returnType != typeof(void))
                    {
                        builder.AppendLine("if (response==null){");
                        if (!em.AllowNullResponse)
                            builder.AppendLine("reject(\"A null response was returned by the server which is invalid.\");");
                        else
                            builder.AppendLine("resolve(response);");
                        builder.AppendLine("}else{");
                        if (new List<Type>(returnType.GetInterfaces()).Contains(typeof(IModel)))
                        {
                            if (array)
                            {
                                builder.AppendLine(string.Format(@"         ret=[];
        for (var x=0;x<response.length;x++){{
            ret.push(_{0}(response[x]));
        }}
        response = ret;", new object[]{
                            returnType.Name
                                }));
                            }
                            else
                            {
                                builder.AppendLine(string.Format(@"             ret = _{0}(response);
        response=ret;", new object[]{
                returnType.Name
                    }));
                            }
                        }
                        builder.AppendLine(@"           resolve(response);
    }");
                    }
                    else
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
