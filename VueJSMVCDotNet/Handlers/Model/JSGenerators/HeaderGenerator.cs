using Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class HeaderGenerator : IBasicJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, string urlBase, sModelType[] models)
        {
            builder.AppendLine(string.Format("import {{isString, isFunction, cloneData, ajax, isEqual, checkProperty, stripBigInt, EventHandler, ModelList, ModelMethods}} from '{0}';", JSHandler.CORE_URL));
        }
    }
}
