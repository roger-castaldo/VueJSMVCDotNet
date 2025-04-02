using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Extensions;

namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators
{
    internal class ModelListCallGenerator : IJSGenerator
    {
        void IJSGenerator.GeneratorJS(WrappedStringBuilder builder, ModelType modelType, string baseURL, ILogger? log)
        {
            modelType.HandlerType.GetMethods(Constants.METHOD_FLAGS)
                .Select(mi => new { Method = mi, ListMethod = mi.GetCustomAttribute<ModelListMethodAttribute>() })
                .Where(gm => gm.ListMethod!=null)
                .ForEach(gm =>
                {
                    log?.LogTrace("Adding List Call[{MethodName}] for Model Definition[{TypeName}]", gm.Method.Name, modelType.Type.FullName);
                    var nna = gm.Method.GetCustomAttribute<NotNullArguementAttribute>(false);
                    var pars = InjectableMethod.StripMethodParameters(gm.Method.GetParameters())
                    .Where(pair=>!pair.IsStrippable)
                    .Select(pair=>pair.ParameterInfo)
                    .ToArray();
                    builder.Append(@$"     static {gm.Method.Name}({string.Join(',', pars.Select(p => p.Name))}){{
            let pars = {{}};
            let changeParameters = function({string.Join(',', pars.Select(p => p.Name))}){{");
                    pars.ForEach(par => builder.AppendLine($"      this.{par.Name} = checkProperty('{par.Name}','{Utility.GetTypeString(par.ParameterType, (nna!=null &&!nna.IsParameterNullable(par)))}',{par.Name},{Utility.GetEnumList(par.ParameterType)});"));
                    builder.AppendLine(@$"           }};
            changeParameters.apply(pars,arguments);
            return new ModelList(
                function(){{ return new {modelType.Type.Name}(); }},
                `${{{modelType.Type.Name}.#baseURL}}/{gm.Method.Name}`,
                {gm.ListMethod!.Paged.ToString().ToLower()},
                false,
                changeParameters,
                pars,
                {Array.Find(pars,p=>p.GetCustomAttribute<PageStartIndexParameterAttribute>()!=null)?.Name??"undefined"},
                {Array.Find(pars, p => p.GetCustomAttribute<PageSizeParameterAttribute>()!=null)?.Name??"undefined"},
                {(!gm.ListMethod!.Paged ? "undefined" : $"{{PageStartIndex:'{Array.Find(pars, p => p.GetCustomAttribute<PageStartIndexParameterAttribute>()!=null)?.Name}',PageSize:'{Array.Find(pars, p => p.GetCustomAttribute<PageSizeParameterAttribute>()!=null)?.Name}'}}")}
            );
        }}");
                });
        }
    }
}
