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
        public void GeneratorJS(ref WrappedStringBuilder builder, bool minimize, Type modelType)
        {
            string urlRoot = Utility.GetModelUrlRoot(modelType);
            builder.AppendLine(string.Format(@"{0}=extend({0},{{Load:function(id){{
        var response = ajax(
        {{
            url:'{2}/'+id,
            type:'GET',
            async:false
        }});
        if (response.ok){{
            var response=response.json();
            return {3}['{1}'](response,new App.Models.{1}());
        }}else{{
            throw response.text();
        }}
    }}
}});", new object[] { Constants.STATICS_VARAIBLE,modelType.Name,urlRoot,Constants.PARSERS_VARIABLE}));
        }
    }
}
