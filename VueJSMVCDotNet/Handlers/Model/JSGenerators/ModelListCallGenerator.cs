using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ModelListCallGenerator : IJSGenerator
    {
        public void GeneratorJS(WrappedStringBuilder builder, SModelType modelType, string? urlBase, ILogger? log)
        {
            modelType.Type.GetMethods(Constants.LOAD_METHOD_FLAGS)
                .Select(mi => new { Method = mi, ListMethod = mi.GetCustomAttribute<ModelListMethodAttribute>() })
                .Where(gm => gm.ListMethod!=null)
                .ForEach(gm =>
                {
                    log?.LogTrace("Adding List Call[{MethodName}] for Model Definition[{TypeName}]", gm.Method.Name, modelType.Type.FullName);
                    var nna = gm.Method.GetCustomAttribute<NotNullArguementAttribute>(false);
                    var pars = InjectableMethod.StripMethodParameters(gm.Method).ToArray();
                    builder.Append($"     static {gm.Method.Name}({string.Join(',', pars.Take((gm.ListMethod!.Paged ? pars.Length-3 : pars.Length)).Select(p => p.Name))}");
                    if (gm.ListMethod!.Paged)
                        builder.Append($"{(pars.Length > 3 ? "," : "")}pageStartIndex,pageSize");

                    builder.AppendLine(@$"){{
            let pars = {{}};
            let changeParameters = function({string.Join(',', pars.Take((gm.ListMethod!.Paged ? pars.Length-3 : pars.Length)).Select(p => p.Name))}){{");
                    pars.SkipLast(gm.ListMethod!.Paged ? 3 : 0).ForEach(par => builder.AppendLine($"      this.{par.Name} = checkProperty('{par.Name}','{Utility.GetTypeString(par.ParameterType, (nna!=null &&!nna.IsParameterNullable(par)))}',{par.Name},{Utility.GetEnumList(par.ParameterType)});"));
                    builder.AppendLine(@$"           }};
            changeParameters.apply(pars,arguments);
            return new ModelList(
                function(){{ return new {modelType.Type.Name}(); }},
                `${{{modelType.Type.Name}.#baseURL}}/{gm.Method.Name}`,
                {gm.ListMethod!.Paged.ToString().ToLower()},
                false,
                changeParameters,
                pars,
                pageStartIndex,
                pageSize,
                {(!gm.ListMethod!.Paged ? "undefined" : $"{{PageStartIndex:'{pars[^3].Name}',PageSize:'{pars[^2].Name}'}}")}
            );
        }}");
                });
        }
    }
}
