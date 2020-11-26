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
        var response = ajax(
        {{
            url:'{1}/'+id,
            type:'GET',
            async:false
        }});
        if (response.ok){{
            var response=response.json();
            if (response==null){{ return null; }}
            var ret = App.Models.{0}.{2}();
            ret.{3}(response);
            return ret;
        }}else{{
            throw response.text();
        }}
    }}
}});", new object[] { modelType.Name,urlRoot,Constants.CREATE_INSTANCE_FUNCTION_NAME,Constants.PARSE_FUNCTION_NAME}));
        }
    }
}
