using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using System;
using System.Reflection;
using System.Text;
using Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSHandler;
using System.Linq;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ModelListCallGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, sModelType modelType, string urlBase)
        {
            foreach (MethodInfo mi in modelType.Type.GetMethods(Constants.LOAD_METHOD_FLAGS).Where(mi => mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0))
            {
                var mlm = mi.GetCustomAttributes().OfType<ModelListMethod>().FirstOrDefault();
                Logger.Trace("Adding List Call[{0}] for Model Definition[{1}]", new object[]
                {
                        mi.Name,
                        modelType.Type.FullName
                });
                NotNullArguement nna = (mi.GetCustomAttributes(typeof(NotNullArguement), false).Length == 0 ? null : (NotNullArguement)mi.GetCustomAttributes(typeof(NotNullArguement), false)[0]);
                builder.AppendFormat(@"     static {0}(", new object[] { mi.Name });
                ParameterInfo[] pars = Utility.ExtractStrippedParameters(mi);

                for (int x = 0; x < (mlm.Paged ? pars.Length - 3 : pars.Length); x++)
                    builder.Append((x > 0 ? "," : "") + pars[x].Name);
                if (mlm.Paged)
                    builder.Append((pars.Length > 3 ? "," : "") + "pageStartIndex,pageSize");

                builder.AppendLine(@"){
            let pars = {};
            let changeParameters = function(");
                for (int x = 0; x < (mlm.Paged ? pars.Length - 3 : pars.Length); x++)
                    builder.Append((x > 0 ? "," : "") + pars[x].Name);
                builder.AppendLine("){");
                for (int x = 0; x < (mlm.Paged ? pars.Length - 3 : pars.Length); x++)
                    builder.AppendLine(string.Format("      this.{0} = checkProperty('{0}','{1}',{0},{2});", new object[]
                        {
                                pars[x].Name,
                                Utility.GetTypeString(pars[x].ParameterType,(nna==null ? false : !nna.IsParameterNullable(pars[x]))),
                                Utility.GetEnumList(pars[x].ParameterType)
                        }));
                builder.AppendLine(string.Format(@"           }};
            changeParameters.apply(pars,arguments);
            return new ModelList(
                function(){{ return new {0}(); }},
                '{1}/{2}',
                {3},
                false,
                changeParameters,
                pars,
                pageStartIndex,
                pageSize,
                {4}
            );
        }}", new object[]{
                modelType.Type.Name,
                Utility.GetModelUrlRoot(modelType.Type),
                mi.Name,
                mlm.Paged.ToString().ToLower(),
                !mlm.Paged ? "undefined" : string.Format("{{PageStartIndex:'{0}',PageSize:'{1}'}}",new object[]
                {
                    pars[pars.Length-3].Name,
                    pars[pars.Length-2].Name
                })
            }));
            }
        }
    }
}
