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

        private static void AppendMethodCallDeclaration(IGrouping<string, MethodInfo> methodGroup, bool isStatic, WrappedStringBuilder builder)
        {
            builder.Append($@"          {(isStatic ? "static async " : "async #")}{methodGroup.Key}(");
            if (methodGroup.Count()==1)
            {
                var method = methodGroup.First();
                var pars = InjectableMethod.StripMethodParameters(method.GetParameters())
                    .Where(pair => !pair.IsStrippable)
                    .Select(pair => pair.ParameterInfo);
                builder.AppendLine($@"{string.Join(',', pars.Select(p => p.Name))}){{
                let opts = {{
                    data:{{}}
                }};");
                var nna = method.GetCustomAttribute<NotNullArguementAttribute>(false);
                pars.ForEach(par =>
                {
                    var propType = Utility.ExtractUnderlyingType(par.ParameterType, out var array, out _, out _);
                    if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)))
                    {
                        if (array)
                            builder.AppendLine($"opts.data.{par.Name} = {par.Name}.map((val)=>{{id:val.id}});");
                        else
                            builder.AppendLine($"opts.data.{par.Name} = {{id:checkProperty('{par.Name}','{Utility.GetTypeString(par.ParameterType, (nna!=null &&!nna.IsParameterNullable(par)))}',{par.Name},{Utility.GetEnumList(par.ParameterType)}).id}};");
                    }
                    else
                        builder.AppendLine($"opts.data.{par.Name} = checkProperty('{par.Name}','{Utility.GetTypeString(par.ParameterType, (nna != null &&!nna.IsParameterNullable(par)))}',{par.Name},{Utility.GetEnumList(par.ParameterType)});");
                });
                AppendMethodReturnCallback(method, builder);
            }
            else
            {
                builder.AppendLine(@"){
                let opts = null;");
                methodGroup.OrderBy(method=> InjectableMethod.StripMethodParameters(method.GetParameters())
                        .Where(pair => !pair.IsStrippable)
                        .Select(pair => pair.ParameterInfo).Count()).Reverse().ForEach(method =>
                {
                    builder.AppendLine(@"               if (opts == null){
                try{
                    opts = { 
                        data:{} 
                    };");
                    var pars = InjectableMethod.StripMethodParameters(method.GetParameters())
                        .Where(pair => !pair.IsStrippable)
                        .Select(pair => pair.ParameterInfo);
                    var nna = method.GetCustomAttribute<NotNullArguementAttribute>(false);
                    pars.ForEach((par,index) =>
                    {
                        builder.AppendLine($"let {par.Name} = {(pars.Count()==1 ? $"arguments[0].{par.Name}??arguments[0]" : $"(arguments.length===1 ? arguments[0].{par.Name} : arguments[{index}])")};");
                        var propType = Utility.ExtractUnderlyingType(par.ParameterType, out var array, out _, out _);
                        if (Array.Exists(propType.GetInterfaces(),t=>Equals(t,typeof(IModel))))
                        {
                            if (array)
                                builder.AppendLine($"opts.data.{par.Name} = {par.Name}.map((val)=>{{id:val.id}});");
                            else
                                builder.AppendLine($"opts.data.{par.Name} = {{id:checkProperty('{par.Name}','{Utility.GetTypeString(par.ParameterType, (nna!=null &&!nna.IsParameterNullable(par)))}',{par.Name},{Utility.GetEnumList(par.ParameterType)}).id}};");
                        }
                        else
                            builder.AppendLine($"opts.data.{par.Name} = checkProperty('{par.Name}','{Utility.GetTypeString(par.ParameterType, (nna != null &&!nna.IsParameterNullable(par)))}',{par.Name},{Utility.GetEnumList(par.ParameterType)});");
                    });
                    AppendMethodReturnCallback(method, builder);
                    builder.AppendLine(@"               } catch(err) { 
                    opts = null;
                    }
                }");
                });
                builder.AppendLine(@"               if (opts===null) { throw new Error('Unable to determine the overload method call to use');  }");
            }
        }

        private static void AppendMethodReturnCallback(MethodInfo method, WrappedStringBuilder builder)
        {
            MethodsGenerator.ExtractReturnType(method, out bool array, out Type returnType, out bool isSlow, out bool allowNullResponse);
            builder.AppendLine(@$"               opts.useJSON = {(method.GetCustomAttributes(typeof(UseFormDataAttribute), false).Length==0
                && !method.GetParameters().Any(p => p.ParameterType==typeof(IFormFile) || p.ParameterType==typeof(IReadOnlyList<IFormFile>))).ToString().ToLower()};
                opts.isSlow = {isSlow.ToString().ToLower()};
                opts.isArray = {array.ToString().ToLower()};
                const returnFunction = function (response) {{
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
            builder.AppendLine("                };");
        }

        private static void AppendMethodContent(ModelType modelType, IGrouping<string,MethodInfo> methodGroup, bool isStatic, WrappedStringBuilder builder)
        {
            MethodsGenerator.AppendMethodCallDeclaration(methodGroup, isStatic, builder);
            builder.AppendLine(@$"let response = await ajax(Object.assign({{
                        url:`${{{modelType.Type.Name}.#baseURL}}/{(isStatic ? methodGroup.Key : $"${{this.{Constants.INITIAL_DATA_KEY}.id}}/{methodGroup.Key}")}`,
                        method:'POST'
                    }}, opts));
                    if (!response.ok)
                        return Promise.reject(response.text());
                    return returnFunction(response);
            }};");
        }

        void IJSGenerator.GeneratorJS(WrappedStringBuilder builder, ModelType modelType, string baseURL, ILogger? log)
        {
            modelType.InstanceMethods.GroupBy(m=>m.Name).ForEach(m => AppendMethodContent(modelType, m, false, builder));
            modelType.StaticMethods.GroupBy(m => m.Name).ForEach(m => AppendMethodContent(modelType, m, true, builder));
        }
    }
}
