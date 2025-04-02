using System.Text.Json;
using System.Text.Json.Serialization;
using VueJSMVCDotNet.Endpoints.DataSources;
using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.Interfaces.Internal;

namespace VueJSMVCDotNet.JSON
{
    internal class ModelConverterFactory(IInternalRequestData? requestData)
        : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
            => Array.Exists(typeToConvert.GetInterfaces(), iface => iface == typeof(IModel));

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions? options)
            => (JsonConverter)Activator.CreateInstance(typeof(ModelConverter<>).MakeGenericType(typeToConvert), requestData)!;
    }
}
