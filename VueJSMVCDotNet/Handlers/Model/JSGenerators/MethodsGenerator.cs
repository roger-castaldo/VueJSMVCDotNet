using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;
using Microsoft.AspNetCore.Http;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class MethodsGenerator : IJSGenerator
    {
        public void GeneratorJS(WrappedStringBuilder builder, SModelType modelType, string urlBase, ILogger log)
        {
            Array.Empty<MethodInfo>()
                .Concat(modelType.InstanceMethods)
                .Concat(modelType.StaticMethods)
                .ForEach(mi =>
                {
                    MethodsGenerator.ExtractReturnType(mi, out bool array, out Type returnType, out bool isSlow, out bool allowNullResponse);
                    MethodsGenerator.AppendMethodCallDeclaration(mi, builder, log);
                    builder.AppendLine(@$"let response = await ajax({{
                        url:`${{{modelType.Type.Name}.#baseURL}}/{(mi.IsStatic ? mi.Name : $"${{this.{Constants.INITIAL_DATA_KEY}.id}}/{mi.Name}")}`,
                        method:'{(mi.IsStatic ? "S" : "")}METHOD',
                        useJSON:{(mi.GetCustomAttributes(typeof(UseFormData), false).Length==0
                        && !mi.GetParameters().Any(p => p.ParameterType==typeof(IFormFile) || p.ParameterType==typeof(IReadOnlyList<IFormFile>))).ToString().ToLower()},
                        data:function_data{(isSlow ? ",isSlow:true,isArray:"+array.ToString().ToLower() : "")}
                    }});
                    if (!response.ok)
                        return Promise.reject(response.text());
                        {(returnType == typeof(void) ? "" : "response = response.json();")}");
                    if (returnType != typeof(void))
                    {
                        builder.AppendLine(@$"if (response==null){{
    {(allowNullResponse ? "return response;" : "return Promise.reject('A null response was returned by the server which is invalid.');")}
}} else {{");
                        if (new List<Type>(returnType.GetInterfaces()).Contains(typeof(IModel)))
                        {
                            if (array)
                                builder.AppendLine($"         response = response.map((r)=>_{returnType.Name}(r));");
                            else
                                builder.AppendLine($"             response = _{returnType.Name}(response);");
                        }
                        builder.AppendLine(@"           return response;
        }");
                    }
                    else
                        builder.AppendLine("           return;");
                    builder.AppendLine(@"
}");
                });
        }

        private static void ExtractReturnType(MethodInfo method, out bool array, out Type returnType, out bool isSlow, out bool allowNullResponse)
        {
            returnType = Utility.ExtractUnderlyingType(method.ReturnType, out array, out _, out _);
            ExposedMethod em = (ExposedMethod)method.GetCustomAttributes(typeof(ExposedMethod), false)[0];
            isSlow=em.IsSlow;
            allowNullResponse=em.AllowNullResponse;
            returnType = (em.ArrayElementType != null ? Array.CreateInstance(em.ArrayElementType, 0).GetType() : returnType);
            array|=em.ArrayElementType!=null;
        }

        private static void AppendMethodCallDeclaration(MethodInfo method, WrappedStringBuilder builder, ILogger log)
        {
            ParameterInfo[] pars = new InjectableMethod(method, log).StrippedParameters;
            builder.AppendLine($@"          {(method.IsStatic ? "static async " : "async #")}{method.Name}({string.Join(',',pars.Select(p=>p.Name))}){{
                let function_data = {{}};");
            NotNullArguement nna = (method.GetCustomAttributes(typeof(NotNullArguement), false).Length == 0 ? null : (NotNullArguement)method.GetCustomAttributes(typeof(NotNullArguement), false)[0]);
            pars.ForEach(par =>
            {
                var propType = Utility.ExtractUnderlyingType(par.ParameterType, out var array, out _, out _);
                if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)))
                {
                    if (array)
                        builder.AppendLine($"function_data.{par.Name} = function_data.{par.Name}.map((val)=>{{id:val.id}});");
                    else
                        builder.AppendLine($"function_data.{par.Name} = {{id:checkProperty('{par.Name}','{Utility.GetTypeString(par.ParameterType, (nna!=null &&!nna.IsParameterNullable(par)))}',{par.Name},{Utility.GetEnumList(par.ParameterType)}).id}};");
                }
                else
                    builder.AppendLine($"function_data.{par.Name} = checkProperty('{par.Name}','{Utility.GetTypeString(par.ParameterType, (nna != null &&!nna.IsParameterNullable(par)))}',{par.Name},{Utility.GetEnumList(par.ParameterType)});");
            });
        }
    }
}
