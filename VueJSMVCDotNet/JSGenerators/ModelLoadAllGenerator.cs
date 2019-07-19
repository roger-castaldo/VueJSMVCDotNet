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
        public void GeneratorJS(ref WrappedStringBuilder builder, bool minimize, Type modelType)
        {
            string urlRoot = Utility.GetModelUrlRoot(modelType);
            foreach (MethodInfo mi in modelType.GetMethods(Constants.LOAD_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelLoadAllMethod), false).Length > 0)
                {
                    builder.AppendLine(string.Format(@"{0}=$.extend({0},{{LoadAll:function(){{
        var ret = $.extend([],{{
            reload:function(){{
                var response = $.ajax({{
                    type:'GET',
                    url:'{2}',
                    dataType:'json',
                    async:false,
                    cache:false
                }});
                if (response.status==200){{
                    var response=JSON.parse(response.responseText);
                    while(this.length>0){{this.pop();}}
                    for(var x=0;x<response.length;x++){{
                        this.push({3}['{1}'](response[x],new App.Models.{1}()));
                    }}
                }}else{{
                    throw response.responseText;
                }}
            }}
        }});
        ret.reload();
        return ret;
    }}
}});", new object[] { Constants.STATICS_VARAIBLE,modelType.Name,urlRoot,Constants.PARSERS_VARIABLE}));
                    break;
                }
            }
        }
    }
}
