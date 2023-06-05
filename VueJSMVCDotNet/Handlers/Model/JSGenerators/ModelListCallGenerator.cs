using VueJSMVCDotNet.Attributes;
using System;
using System.Reflection;
using System.Text;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;
using System.Linq;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ModelListCallGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, sModelType modelType, string urlBase, ILog log)
        {
            foreach (MethodInfo mi in modelType.Type.GetMethods(Constants.LOAD_METHOD_FLAGS).Where(mi => mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0))
            {
                var mlm = mi.GetCustomAttributes().OfType<ModelListMethod>().FirstOrDefault();
                log.Trace("Adding List Call[{0}] for Model Definition[{1}]", new object[]
                {
                        mi.Name,
                        modelType.Type.FullName
                });
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
                for (int x = 0; x < (mlm.Paged ? pars.Length - 3 : pars.Length); x++)
                    builder.AppendLine($"      this.{pars[x].Name} = checkProperty('{pars[x].Name}','{Utility.GetTypeString(pars[x].ParameterType, (nna==null ? false : !nna.IsParameterNullable(pars[x])))}',{pars[x].Name},{Utility.GetEnumList(pars[x].ParameterType)});");
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
                {(!mlm.Paged ? "undefined" : $"{{PageStartIndex:'{pars[pars.Length-3].Name}',PageSize:'{pars[pars.Length-2].Name}'}}")}
            );
        }}");
            }
        }
    }
}
