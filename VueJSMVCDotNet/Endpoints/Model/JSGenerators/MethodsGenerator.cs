using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Extensions;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators
{
    internal class MethodsGenerator : IJSGenerator
    {
        private static void ExtractReturnType(MethodInfo method, out bool array, out Type returnType, out bool isSlow, out bool allowNullResponse)
        {
            returnType = Utility.ExtractUnderlyingType(method.ReturnType, out array, out _, out _);
            ExposedMethodAttribute em = (ExposedMethodAttribute)method.GetCustomAttributes(typeof(ExposedMethodAttribute), false)[0];
            isSlow=em.IsSlow;
            allowNullResponse=em.AllowNullResponse;
            returnType = (em.ArrayElementType != null ? Array.CreateInstance(em.ArrayElementType, 0).GetType() : returnType);
            array|=em.ArrayElementType!=null;
        }

        private static void AppendMethodCallDeclaration(MethodInfo method, WrappedStringBuilder builder)
        {
            var pars = InjectableMethod.StripMethodParameters(method.GetParameters())
                .Where(pair=>!pair.IsStrippable)
                .Select(pair=>pair.ParameterInfo);
            builder.AppendLine($@"          {(method.IsStatic ? "static async " : "async #")}{method.Name}({string.Join(',', pars.Select(p => p.Name))}){{
                let function_data = {{}};");
            var nna = method.GetCustomAttribute<NotNullArguementAttribute>(false);
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

        private static void AppendMethodContent(ModelType modelType, MethodInfo mi,bool isStatic,WrappedStringBuilder builder)
        {
            MethodsGenerator.ExtractReturnType(mi, out bool array, out Type returnType, out bool isSlow, out bool allowNullResponse);
            MethodsGenerator.AppendMethodCallDeclaration(mi, builder);
            builder.AppendLine(@$"let response = await ajax({{
                        url:`${{{modelType.Type.Name}.#baseURL}}/{(isStatic ? mi.Name : $"${{this.{Constants.INITIAL_DATA_KEY}.id}}/{mi.Name}")}`,
                        method:'POST',
                        useJSON:{(mi.GetCustomAttributes(typeof(UseFormDataAttribute), false).Length==0
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
        }

        void IJSGenerator.GeneratorJS(WrappedStringBuilder builder, ModelType modelType, string baseURL, ILogger? log)
        {
            modelType.InstanceMethods.ForEach(m => AppendMethodContent(modelType, m, false, builder));
            modelType.StaticMethods.ForEach(m => AppendMethodContent(modelType, m, true, builder));
        }
    }
}
