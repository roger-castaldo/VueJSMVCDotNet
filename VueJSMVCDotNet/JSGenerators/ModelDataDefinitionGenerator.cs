using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class ModelDataDefinitionGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType, string modelNamespace, string urlBase)
        {
            Logger.Trace("Generating Model Data Definition javascript for {0}", new object[] { modelType.FullName });
            string urlRoot = Utility.GetModelUrlRoot(modelType, urlBase);
            List<PropertyInfo> props = Utility.GetModelProperties(modelType);
            Logger.Trace("Adding data method for Model Definition[{0}]", new object[] { modelType.FullName });
            IModel m = null;
            if (modelType.GetConstructor(Type.EmptyTypes) != null)
            {
                m = (IModel)modelType.GetConstructor(Type.EmptyTypes).Invoke(new object[] { });
            }
            builder.AppendLine(@"    data = _defineTypedObject({");
            bool isFirst = true;
            foreach (PropertyInfo pi in props)
            {
                if (pi.CanRead && pi.CanWrite)
                {
                    builder.Append(string.Format(@"{0}
            {1}:{{
                initial:{2},
                type:'{3}',
                enumlist:{4}
            }}", new object[]
                    {
                        (isFirst ? "" : ","),
                        pi.Name,
                        (m==null ? "null" : (pi.GetValue(m,new object[0])==null ? "null" : JSON.JsonEncode(pi.GetValue(m,new object[0])))),
                        Utility.GetTypeString(pi.PropertyType,pi.GetCustomAttributes(typeof(NotNullProperty),false).Length>0),
                        Utility.GetEnumList(pi.PropertyType)
                    }));
                    isFirst = false;
                }
            }
            builder.AppendLine(@"
    });");
        }
    }
}
