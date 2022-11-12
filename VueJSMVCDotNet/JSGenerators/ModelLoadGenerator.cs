using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.JSHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class ModelLoadGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, sModelType modelType, string urlBase)
        {
            Logger.Trace("Appending Model Load method for Model Definition[{0}]", new object[] { modelType.Type.FullName });
            builder.AppendLine(string.Format(@"     static Load(id,callback){{
        let ret = new {0}();
        ret.{1}({{id:id}});
        if (callback!=undefined){{
            ret.reload().then(
                model=>{{callback(model);}},
                errored=>{{callback(null);}}
            );
        }}else{{
            ret.reload();
        }}
        return ret;
    }}", new object[] { modelType.Type.Name,Constants.PARSE_FUNCTION_NAME}));
        }
    }
}
