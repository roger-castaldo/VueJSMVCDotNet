using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    class ModelInstanceHeaderGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType)
        {
            builder.AppendLine(string.Format(@"     App.Models.{0} = App.Models.{0}||{{}};
        App.Models.{0}.{1} = function(){{ 
            var methods = {{}};
            var data = {{}};
            var computed = {{}};",modelType.Name,Constants.CREATE_INSTANCE_FUNCTION_NAME));
        }
    }
}
