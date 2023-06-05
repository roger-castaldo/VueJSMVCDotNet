using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class MethodsGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, sModelType modelType, string urlBase, ILog log)
        {
            List<MethodInfo> methods = new List<MethodInfo>();
            methods.AddRange(modelType.InstanceMethods);
            methods.AddRange(modelType.StaticMethods);
            foreach (MethodInfo mi in methods)
            {
                Type returnType;
                bool array;
                bool isSlow;
                bool allowNullResponse;
                _ExtractReturnType(mi, out array, out returnType, out isSlow, out allowNullResponse);
                _AppendMethodCallDeclaration(mi, ref builder,log);
                builder.AppendLine(@$"let response = await ajax({{
                        url:`${{{modelType.Type.Name}.#baseURL}}/{(mi.IsStatic ? mi.Name : $"${{this.{Constants.INITIAL_DATA_KEY}.id}}/{mi.Name}")}`,
                        method:'{(mi.IsStatic ? "S" : "")}METHOD',
                        useJSON:{(mi.GetCustomAttributes(typeof(UseFormData), false).Length==0).ToString().ToLower()},
                        data:function_data{(isSlow ? ",isSlow:true,isArray:"+array.ToString().ToLower() : "")}
                    }});
                        {(returnType == typeof(void) ? "" : @"let ret=response.json();
                    if (ret!=undefined||ret==null)
                        response = ret;")}");
                if (returnType != typeof(void))
                {
                    builder.AppendLine(@$"if (response==null){{
    {(allowNullResponse ? "return response;" : "return Promise.reject('A null response was returned by the server which is invalid.');")}
}} else {{");
                    if (new List<Type>(returnType.GetInterfaces()).Contains(typeof(IModel)))
                    {
                        if (array)
                        {
                            builder.AppendLine(@$"         ret=[];
            for (let x=0;x<response.length;x++){{
                ret.push(_{returnType.Name}(response[x]));
            }}
            response = ret;");
                        }
                        else
                        {
                            builder.AppendLine(@$"             ret = _{returnType.Name}(response);
            response=ret;");
                        }
                    }
                    builder.AppendLine(@"           return response;
        }");
                }
                else
                    builder.AppendLine("           return;");
                builder.AppendLine(@"
}");
            }
        }

        private void _ExtractReturnType(MethodInfo method, out bool array, out Type returnType, out bool isSlow, out bool allowNullResponse)
        {
            ExposedMethod em = (ExposedMethod)method.GetCustomAttributes(typeof(ExposedMethod), false)[0];
            isSlow=em.IsSlow;
            allowNullResponse=em.AllowNullResponse;
            returnType = (em.ArrayElementType != null ? Array.CreateInstance(em.ArrayElementType, 0).GetType() : method.ReturnType);
            array = false;
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
        }

        private void _AppendMethodCallDeclaration(MethodInfo method, ref WrappedStringBuilder builder, ILog log)
        {
            builder.Append($"          {(method.IsStatic ? "static async " : "async #")}{method.Name}(");
            ParameterInfo[] pars = new InjectableMethod(method,log).StrippedParameters;
            for (int x = 0; x < pars.Length; x++)
                builder.Append($"{pars[x].Name}{(x + 1 == pars.Length ? "" : ",")}");
            builder.AppendLine(@"){
                let function_data = {};");
            NotNullArguement nna = (method.GetCustomAttributes(typeof(NotNullArguement), false).Length == 0 ? null : (NotNullArguement)method.GetCustomAttributes(typeof(NotNullArguement), false)[0]);
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
                        builder.AppendLine(@$"function_data.{par.Name}=[];
for(let x=0;x<{par.Name}.length;x++){{
    function_data.{par.Name}.push({{id:{par.Name}[x].id}});
}}");
                    }
                    else
                        builder.AppendLine($"function_data.{par.Name} = checkProperty('{par.Name}','{Utility.GetTypeString(par.ParameterType, (nna==null ? false : !nna.IsParameterNullable(par)))}',{par.Name},{Utility.GetEnumList(par.ParameterType)});");
                }
                else
                    builder.AppendLine($"function_data.{par.Name} = checkProperty('{par.Name}','{Utility.GetTypeString(par.ParameterType, (nna == null ? false : !nna.IsParameterNullable(par)))}',{par.Name},{Utility.GetEnumList(par.ParameterType)});");
            }
        }
    }
}
