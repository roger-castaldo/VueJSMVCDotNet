using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class ExtendMethodGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, bool minimize, Type modelType)
        {
            builder.AppendLine(string.Format(@" var ext = function(clazz,data){{
        var ret = (clazz._extend==undefined ? clazz.extend(data) : clazz._extend(data));
        ret._extend=clazz.extend;
        ret.extend = function(data){{
            return ext(this,data);
        }};
        return $.extend(ret,{1});
    }};
    App.Models.{0}=ext(App.Models.{0},{{}});", new object[] { modelType.Name,Constants.STATICS_VARAIBLE}));
        }
    }
}
