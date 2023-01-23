using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using System;
using System.Reflection;
using System.Text;
using Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ModelListCallGenerator : IJSGenerator
    {
        private static string _CreateJavacriptUrlCode(ModelListMethod mlm, ParameterInfo[] pars, sModelType modelType,string urlBase)
        {
            Logger.Trace("Creating the javascript url call for the model list method at path {0}",new object[] { mlm.Path });
            if (pars.Length > 0)
            {
                string[] pNames = new string[pars.Length];
                for (int x = 0; x < (mlm.Paged ? pars.Length - 3 : pars.Length); x++)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("'+(");
                    if (pars[x].ParameterType == typeof(bool))
                        sb.AppendFormat("pars.{0}==undefined ? 'false' : (pars.{0}==null ? 'false' : (pars.{0} ? 'true' : 'false')))+'", pars[x].Name);
                    else if (pars[x].ParameterType == typeof(DateTime))
                        sb.AppendFormat("pars.{0}==undefined ? 'NULL' : (pars.{0}==null ? 'NULL' : Date.UTC(pars.{0}.getUTCFullYear(), pars.{0}.getUTCMonth(), pars.{0}.getUTCDate(), pars.{0}.getUTCHours(), pars.{0}.getUTCMinutes(), pars.{0}.getUTCSeconds())))+'", pars[x].Name);
                    else
                        sb.AppendFormat("pars.{0}==undefined ? 'NULL' : (pars.{0} == null ? 'NULL' : encodeURI(pars.{0})))+'", pars[x].Name);
                    pNames[x] = sb.ToString();
                }
                return "'" + (urlBase==null ? null : urlBase)+string.Format((mlm.Path.StartsWith("/") ? mlm.Path : "/" + mlm.Path).TrimEnd('/'), pNames) + "'";
            }
            else
                return "'"  + (urlBase==null ? null : urlBase)+ (mlm.Path.StartsWith("/") ? mlm.Path : "/" + mlm.Path).TrimEnd('/') + "'";
        }

        public void GeneratorJS(ref WrappedStringBuilder builder, sModelType modelType, string urlBase)
        {
            foreach (MethodInfo mi in modelType.Type.GetMethods(Constants.LOAD_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0)
                {
                    Logger.Trace("Adding List Call[{0}] for Model Definition[{1}]", new object[]
                    {
                        mi.Name,
                        modelType.Type.FullName
                    });
                    ModelListMethod mlm = (ModelListMethod)mi.GetCustomAttributes(typeof(ModelListMethod), false)[0];
                    NotNullArguement nna = (mi.GetCustomAttributes(typeof(NotNullArguement), false).Length == 0 ? null : (NotNullArguement)mi.GetCustomAttributes(typeof(NotNullArguement), false)[0]);
                    builder.AppendFormat(@"     static {0}(", new object[] { mi.Name });
                    ParameterInfo[] pars = Utility.ExtractStrippedParameters(mi);

                    string url = _CreateJavacriptUrlCode(mlm, pars, modelType,urlBase);
                    if (mlm.Paged)
                        url += string.Format("+'{0}PageStartIndex='+currentIndex+'&PageSize='+currentPageSize", (mlm.Path.Contains("?") ? "&" : "?"));

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
                function(params,currentIndex,currentPageSize){{
                    return {1};
                }},
                {2},
                changeParameters,
                pars,
                pageStartIndex,
                pageSize
            );
        }}",new object[]{
                modelType.Type.Name,
                url,
                mlm.Paged.ToString().ToLower()
            }));
                }
            }
        }
    }
}
