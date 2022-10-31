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
        let ret = secureArray(extend([],{{
            {0}
            {1}
            {2}
        }}));
        ret.reload();
        return ret;
    }}", new object[] {
                        Constants._LIST_EVENTS_CODE,
                        Constants.ARRAY_TO_VUE_METHOD,
                        Constants._LIST_RELOAD_CODE.Replace("$url$", string.Format("'{0}'",urlRoot)).Replace("$type$", modelType.Name)
                    }));
                    break;
                }
            }
        }
    }
}
