using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ModelListCallGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, SModelType modelType, string urlBase, ILogger log)
        {
            foreach (MethodInfo mi in modelType.Type.GetMethods(Constants.LOAD_METHOD_FLAGS).Where(mi => mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0))
            {
                var mlm = mi.GetCustomAttributes().OfType<ModelListMethod>().FirstOrDefault();
                log?.LogTrace("Adding List Call[{}] for Model Definition[{}]", mi.Name,modelType.Type.FullName);
                NotNullArguement nna = (mi.GetCustomAttributes(typeof(NotNullArguement), false).Length == 0 ? null : (NotNullArguement)mi.GetCustomAttributes(typeof(NotNullArguement), false)[0]);
                builder.Append($"     static {mi.Name}(");
                ParameterInfo[] pars = new InjectableMethod(mi,log).StrippedParameters;

                for (int x = 0; x < (mlm.Paged ? pars.Length - 3 : pars.Length); x++)
                    builder.Append($"{(x > 0 ? "," : "")}{pars[x].Name}");
                if (mlm.Paged)
                    builder.Append($"{(pars.Length > 3 ? "," : "")}pageStartIndex,pageSize");

                builder.AppendLine(@"){
            let pars = {};
            let changeParameters = function(");
                for (int x = 0; x < (mlm.Paged ? pars.Length - 3 : pars.Length); x++)
                    builder.Append($"{(x > 0 ? "," : "")}{pars[x].Name}");
                builder.AppendLine("){");
                foreach (var par in pars.SkipLast(mlm.Paged?3:0))
                    builder.AppendLine($"      this.{par.Name} = checkProperty('{par.Name}','{Utility.GetTypeString(par.ParameterType, (nna!=null &&!nna.IsParameterNullable(par)))}',{par.Name},{Utility.GetEnumList(par.ParameterType)});");
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
            }
        }
    }
}
