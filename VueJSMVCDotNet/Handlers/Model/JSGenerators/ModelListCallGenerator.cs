using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ModelListCallGenerator : IJSGenerator
    {
        public void GeneratorJS(WrappedStringBuilder builder, SModelType modelType, string urlBase, ILogger log)
        {
            modelType.Type.GetMethods(Constants.LOAD_METHOD_FLAGS)
                .Where(mi => mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0)
                .ForEach(mi =>
                {
                    var mlm = mi.GetCustomAttributes().OfType<ModelListMethod>().FirstOrDefault();
                    log?.LogTrace("Adding List Call[{}] for Model Definition[{}]", mi.Name, modelType.Type.FullName);
                    NotNullArguement nna = (mi.GetCustomAttributes(typeof(NotNullArguement), false).Length == 0 ? null : (NotNullArguement)mi.GetCustomAttributes(typeof(NotNullArguement), false)[0]);
                    ParameterInfo[] pars = new InjectableMethod(mi, log).StrippedParameters;
                    builder.Append($"     static {mi.Name}({string.Join(',',pars.Take((mlm.Paged?pars.Length-3:pars.Length)).Select(p=>p.Name))}");
                    if (mlm.Paged)
                        builder.Append($"{(pars.Length > 3 ? "," : "")}pageStartIndex,pageSize");

                    builder.AppendLine(@$"){{
            let pars = {{}};
            let changeParameters = function({string.Join(',', pars.Take((mlm.Paged ? pars.Length-3 : pars.Length)).Select(p => p.Name))}){{");
                    pars.SkipLast(mlm.Paged ? 3 : 0).ForEach(par => builder.AppendLine($"      this.{par.Name} = checkProperty('{par.Name}','{Utility.GetTypeString(par.ParameterType, (nna!=null &&!nna.IsParameterNullable(par)))}',{par.Name},{Utility.GetEnumList(par.ParameterType)});"));
                    builder.AppendLine(@$"           }};
            changeParameters.apply(pars,arguments);
            return new ModelList(
                function(){{ return new {modelType.Type.Name}(); }},
                '{Utility.GetModelUrlRoot(modelType.Type)}/{mi.Name}',
                {mlm.Paged.ToString().ToLower()},
                false,
                changeParameters,
                pars,
                pageStartIndex,
                pageSize,
                {(!mlm.Paged ? "undefined" : $"{{PageStartIndex:'{pars[^3].Name}',PageSize:'{pars[^2].Name}'}}")}
            );
        }}");
                });
        }
    }
}
