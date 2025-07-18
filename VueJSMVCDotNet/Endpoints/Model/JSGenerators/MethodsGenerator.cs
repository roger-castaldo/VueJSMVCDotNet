using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Extensions;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators
{
    internal class MethodsGenerator : IJSGenerator
    {
        private static (Type returnType, bool array, bool isSlow, bool allowNullResponse) ExtractReturnType(MethodInfo method)
        {
            (var returnType, var array, _, _, _) = Utility.ExtractUnderlyingType(method.ReturnType);
            ExposedMethodAttribute em = (ExposedMethodAttribute)method.GetCustomAttributes(typeof(ExposedMethodAttribute), false)[0];
            returnType = (em.ArrayElementType != null ? Array.CreateInstance(em.ArrayElementType, 0).GetType() : returnType);
            array|=em.ArrayElementType!=null;
            return (returnType, array, em.IsSlow, em.AllowNullResponse);
        }

        private static void AppendMethodCallDeclaration(IGrouping<string, MethodInfo> methodGroup, bool isStatic, StringBuilder builder)
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
                AppendMethodParameters(pars, nna, builder);
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
                    AppendMethodParameters(pars, nna, builder, true);
                    AppendMethodReturnCallback(method, builder);
                    builder.AppendLine(@"               } catch(err) { 
                    opts = null;
                    }
                }");
                });
                builder.AppendLine(@"               if (opts===null) { throw new Error('Unable to determine the overload method call to use');  }");
            }
        }

        private static void AppendMethodParameters(IEnumerable<ParameterInfo> pars, NotNullArguementAttribute? nna, StringBuilder builder, bool useArguements = false)
        {
            pars.ForEach((par, index) =>
            {
                if (useArguements)
                    builder.AppendLine($"let {par.Name} = {(pars.Count()==1 ? $"arguments[0].{par.Name}??arguments[0]" : $"(arguments.length===1 ? arguments[0].{par.Name} : arguments[{index}])")};");
                (var propType, var array, var isNullable, _, _) = Utility.ExtractUnderlyingType(par.ParameterType);
                bool notNullTagged = nna!=null && !nna.IsParameterNullable(par);
                if (Array.Exists(propType.GetInterfaces(), t => Equals(t, typeof(IModel))))
                {
                    if (array)
                    {
                        if (notNullTagged || !isNullable)
                            builder.AppendLine($"if ({par.Name}===null) throw 'invalid type: {par.Name} is not allowed to be null';");
                        builder.AppendLine($"opts.data.{par.Name} = ({par.Name}===null ? null : {par.Name}.map((val)=>{{id:val.id}}));");
                    }
                    else
                        builder.AppendLine($"opts.data.{par.Name} = {{id:checkProperty('{par.Name}','{Utility.GetTypeString(typeof(string), (nna!=null &&!nna.IsParameterNullable(par)))}',{par.Name}?.id,{Utility.GetEnumList(par.ParameterType)})}};");
                }
                else
                    builder.AppendLine($"opts.data.{par.Name} = checkProperty('{par.Name}','{Utility.GetTypeString(par.ParameterType, (nna != null &&!nna.IsParameterNullable(par)))}',{par.Name},{Utility.GetEnumList(par.ParameterType)});");
            });
        }

        private static void AppendMethodReturnCallback(MethodInfo method, StringBuilder builder)
        {
            (var returnType, var array, var isSlow, var allowNullResponse) = MethodsGenerator.ExtractReturnType(method);
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

        private static void AppendMethodContent(ModelType modelType, IGrouping<string,MethodInfo> methodGroup, bool isStatic, StringBuilder builder)
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

        void IJSGenerator.GeneratorJS(StringBuilder builder, ModelType modelType, string baseURL, ILogger? log)
        {
            modelType.InstanceMethods.GroupBy(m=>m.Name).ForEach(m => AppendMethodContent(modelType, m, false, builder));
            modelType.StaticMethods.GroupBy(m => m.Name).ForEach(m => AppendMethodContent(modelType, m, true, builder));
        }
    }
}
