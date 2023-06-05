using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class FooterGenerator : IBasicJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, string urlBase, sModelType[] models, ILog log)
        {
            builder.Append("export {");
            foreach (sModelType type in models)
                builder.Append($"{type.Type.Name},");
            builder.Length=builder.Length-1;
            builder.AppendLine("}");
        }
    }
}
