using VueJSMVCDotNet.Attributes;
using System.Reflection;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ModelLoadAllGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, sModelType modelType, string urlBase)
        {
            foreach (MethodInfo mi in modelType.Type.GetMethods(Constants.LOAD_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelLoadAllMethod), false).Length > 0)
                {
                    Logger.Trace("Adding Load All Method for Model Definition[{0}]", new object[] { modelType.Type.FullName });
                    builder.AppendLine(string.Format(@"     static LoadAll(){{
                            var ret = new ModelList(
                                function(){{ return new {0}(); }},
                                {0}.#baseURL,
                                false,
                                true,
                                undefined
                            );
                            ret.reload();
                            return ret;
                        }}",new object[]{
                            modelType.Type.Name
                        }));
                    break;
                }
            }
        }
    }
}
