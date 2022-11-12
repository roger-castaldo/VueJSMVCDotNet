using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.JSHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
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
                                function(){{ return {0}.#baseURL; }},
                                false,
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
