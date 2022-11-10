using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class ModelLoadAllGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType, string urlBase)
        {
            string urlRoot = Utility.GetModelUrlRoot(modelType,urlBase);
            foreach (MethodInfo mi in modelType.GetMethods(Constants.LOAD_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelLoadAllMethod), false).Length > 0)
                {
                    Logger.Trace("Adding Load All Method for Model Definition[{0}]", new object[] { modelType.FullName });
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
                            modelType.Name
                        }));
                    break;
                }
            }
        }
    }
}
