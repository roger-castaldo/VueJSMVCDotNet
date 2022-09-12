using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class ModelLoadGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType, string modelNamespace, string urlBase)
        {
            Logger.Trace("Appending Model Load method for Model Definition[{0}]", new object[] { modelType.FullName });
            builder.AppendLine(string.Format(@"{0}.{1}=extend({0}.{1},{{Load:function(id,callback){{
        var ret = {0}.{1}.{2}();
        ret.{3}({{id:id}});
        if (callback!=undefined){{
            ret.reload().then(
                model=>{{callback(model);}},
                errored=>{{callback(null);}}
            );
        }}else{{
            ret.reload();
        }}
        return ret;
    }}
}});", new object[] { modelNamespace,modelType.Name,Constants.CREATE_INSTANCE_FUNCTION_NAME,Constants.PARSE_FUNCTION_NAME}));
        }
    }
}
