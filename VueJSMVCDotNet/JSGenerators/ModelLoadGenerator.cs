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
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType)
        {
            string urlRoot = Utility.GetModelUrlRoot(modelType);
            builder.AppendLine(string.Format(@"App.Models.{0}=extend(App.Models.{0},{{Load:function(id){{
        var ret = App.Models.{0}.{1}();
        ret.{2}({{id:id}});
        ret.reload();
        return ret;
    }}
}});", new object[] { modelType.Name,Constants.CREATE_INSTANCE_FUNCTION_NAME,Constants.PARSE_FUNCTION_NAME}));
        }
    }
}
