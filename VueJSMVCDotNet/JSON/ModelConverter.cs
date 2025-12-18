using System.Text.Json;
using System.Text.Json.Serialization;
using VueJSMVCDotNet.Attributes.Models;
using VueJSMVCDotNet.Extensions;
using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.Interfaces.Internal;

namespace VueJSMVCDotNet.JSON
{
    internal class ModelConverter<M>(IInternalRequestData? requestData)
        : JsonConverter<M> where M : IModel
    {
        private M Load(string id)
        {
            RequestDataNullException.ThrowIfNull(requestData);
            var task = requestData!.LoadModelAsync<M>(id).AsTask();
            task.Wait();
            return task.Result!;
        }

        public override M? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = default(M?);
            if (reader.TokenType==JsonTokenType.String)
                result = Load(reader.GetString()!);
            else if (reader.TokenType==JsonTokenType.StartObject)
            {
                reader.Read();
                string pid = reader.GetString()!;
                if (pid=="id")
                {
                    reader.Read();
                    result = Load(reader.GetString()!);
                    reader.Read();
                }
                else
                {
                    result = Activator.CreateInstance<M>();
                    while (reader.TokenType!=JsonTokenType.EndObject)
                    {
                        var prop = typeof(M).GetProperty(reader.GetString()!);
                        reader.Read();
                        prop?.SetValue(result, JsonSerializer.Deserialize(ref reader, prop.PropertyType, options));
                    }
                    reader.Read();
                }
            }
            return result;
        }

        public override void Write(Utf8JsonWriter writer, M? value, JsonSerializerOptions options)
        {
            if (Equals(value, default(M?)))
                writer.WriteNullValue();
            else
            {
                writer.WriteStartObject();

                value!.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                    .Where(p =>
                        p.GetCustomAttribute<ModelIgnorePropertyAttribute>(false)==null
                        && !(p.PropertyType.FullName?.Contains("+KeyCollection")??false)
                        && (p.GetGetMethod()?.GetParameters()?? []).Length == 0)
                    .ForEach(pi =>
                    {
                        writer.WritePropertyName(pi.Name);
                        JsonSerializer.Serialize(writer, pi.GetValue(value), pi.PropertyType, options);
                    });

                writer.WriteEndObject();
            }
        }
    }
}
