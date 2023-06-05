using VueJSMVCDotNet.Attributes;
using System.Reflection;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ModelLoadAllGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, sModelType modelType, string urlBase, ILog log)
        {
            foreach (MethodInfo mi in modelType.Type.GetMethods(Constants.LOAD_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelLoadAllMethod), false).Length > 0)
                {
                    log.Trace("Adding Load All Method for Model Definition[{0}]", new object[] { modelType.Type.FullName });
                    builder.AppendLine(@$"     static LoadAll(){{
                            var ret = new ModelList(
                                function(){{ return new {modelType.Type.Name}(); }},
                                {modelType.Type.Name}.#baseURL,
                                false,
                                true,
                                undefined
                            );
                            ret.reload();
                            return ret;
                        }}");
                    break;
                }
            }
        }
    }
}
