using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class MethodsGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType, string urlBase)
        {
            List<MethodInfo> methods = new List<MethodInfo>();
            methods.AddRange(Utility.GetModelMethods(modelType, false));
            methods.AddRange(Utility.GetModelMethods(modelType, true));
            foreach (MethodInfo mi in methods)
            {
                Type returnType;
                bool array;
                bool isSlow;
                bool allowNullResponse;
                _ExtractReturnType(mi, out array, out returnType, out isSlow, out allowNullResponse);
                _AppendMethodCallDeclaration(mi, ref builder);
                if (!mi.IsStatic)
                    builder.AppendLine("            let model=this;");
                builder.AppendFormat(@"return new Promise((resolve,reject)=>{{
                    ajax(
                    {{
                        url:{0}.#baseURL+'/'+{1},
                        method:'{2}METHOD',
                        useJSON:{3},
                        data:function_data{4}
                    }}).then(response=>{{
                        {5}",
                        new object[]
                        {
                            modelType.Name,
                            string.Format((mi.IsStatic ? "'{0}'" : "model.{1}.id+'/{0}'"),new object[]{mi.Name,Constants.INITIAL_DATA_KEY}),
                            (mi.IsStatic ? "S":""),
                            (mi.GetCustomAttributes(typeof(UseFormData),false).Length==0).ToString().ToLower(),
                            (isSlow ? ",isSlow:true,isArray:"+array.ToString().ToLower() : ""),
                            (returnType == typeof(void) ? "" : @"let ret=response.json();
                    if (ret!=undefined||ret==null)
                        response = ret;")
                        });
                if (returnType != typeof(void))
                {
                    builder.AppendLine("if (response==null){");
                    if (!allowNullResponse)
                        builder.AppendLine("reject(\"A null response was returned by the server which is invalid.\");");
                    else
                        builder.AppendLine("resolve(response);");
                    builder.AppendLine("}else{");
                    if (new List<Type>(returnType.GetInterfaces()).Contains(typeof(IModel)))
                    {
                        if (array)
                        {
                            builder.AppendLine(string.Format(@"         ret=[];
            for (let x=0;x<response.length;x++){{
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

        private void _AppendMethodCallDeclaration(MethodInfo method, ref WrappedStringBuilder builder)
        {
            builder.AppendFormat("          {0}{1}(", new object[] { (method.IsStatic ? "static " : "#"), method.Name });
            ParameterInfo[] pars = Utility.ExtractStrippedParameters(method);
            for (int x = 0; x < pars.Length; x++)
                builder.Append(pars[x].Name + (x + 1 == pars.Length ? "" : ","));
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
                        builder.AppendLine(string.Format(@"function_data.{0}=[];
for(let x=0;x<{0}.length;x++){{
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
                                Utility.GetTypeString(par.ParameterType, (nna == null ? false : ! nna.IsParameterNullable(par))),
                                Utility.GetEnumList(par.ParameterType)
                    }));
            }
        }
    }
}
