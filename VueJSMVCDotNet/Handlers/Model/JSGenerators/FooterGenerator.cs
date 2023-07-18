using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class FooterGenerator : IBasicJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, string urlBase, SModelType[] models, ILogger log)
        {
            builder.Append("export {");
            foreach (SModelType type in models)
                builder.Append($"{type.Type.Name},");
            builder.Length--;
            builder.AppendLine("}");
        }
    }
}
