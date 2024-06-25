using System.Text.Json;
using System.Text.Json.Serialization;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.JSON
{
    internal class ModelConverter<T>(IRequestData requestData)
        : JsonConverter<T> where T : IModel
    {
        private readonly IRequestData requestData = requestData;
        private readonly InjectableMethod loadMethod = new(typeof(T).GetMethods(Constants.LOAD_METHOD_FLAGS).First(m => m.GetCustomAttributes(typeof(ModelLoadMethodAttribute)).Any()));

        private T Load(string id)
        {
            var task = loadMethod.InvokeAsync<T>(null, requestData, pars: [id]);
            task.Wait();
            return task.Result!;
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = default(T);
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
                    result = Activator.CreateInstance<T>();
                    while (reader.TokenType!=JsonTokenType.EndObject)
                    {
                        var prop = typeof(T).GetProperty(reader.GetString()!);
                        reader.Read();
                        prop?.SetValue(result, JsonSerializer.Deserialize(ref reader, prop.PropertyType, options));
                    }
                    reader.Read();
                }
            }
            return result;
        }

        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            if (Equals(value, default(T?)))
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
