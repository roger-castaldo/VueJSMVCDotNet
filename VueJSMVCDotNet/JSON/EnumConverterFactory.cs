using System.Text.Json;
using System.Text.Json.Serialization;

namespace VueJSMVCDotNet.JSON
{
    internal class EnumConverterFactory : JsonConverterFactory
    {

        public override bool CanConvert(Type typeToConvert)
            =>typeToConvert.IsEnum;

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            => (JsonConverter)Activator.CreateInstance(typeof(EnumConverter<>).MakeGenericType(new Type[] {typeToConvert}));
    }
}
