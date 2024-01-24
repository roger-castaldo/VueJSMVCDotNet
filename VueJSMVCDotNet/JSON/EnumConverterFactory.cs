using System.Text.Json;
using System.Text.Json.Serialization;

namespace VueJSMVCDotNet.JSON
{
    internal class EnumConverterFactory : JsonConverterFactory
    {
        public EnumConverterFactory()
        {
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsEnum;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return (JsonConverter)Activator.CreateInstance(typeof(EnumConverter<>).MakeGenericType(new Type[] {typeToConvert}));
        }
    }
}
