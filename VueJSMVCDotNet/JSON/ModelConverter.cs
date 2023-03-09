using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.JSON
{
    internal class ModelConverter<T> : JsonConverter<T> where T :IModel
    {
        private readonly ISecureSession _session;
        private readonly InjectableMethod _loadMethod;
        public ModelConverter(ISecureSession session)
        {
            _session=session;
            _loadMethod = new InjectableMethod(typeof(T).GetMethods(Constants.LOAD_METHOD_FLAGS).FirstOrDefault(m => m.GetCustomAttributes(typeof(ModelLoadMethod)).Any()));
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = default(T);
            if (reader.TokenType==JsonTokenType.String)
                result = (T)_loadMethod.Invoke(null,pars:new object[] { reader.GetString() }, session:_session);
            else if (reader.TokenType==JsonTokenType.StartObject)
            {
                reader.Read();
                string pid = reader.GetString();
                if (pid=="id")
                {
                    reader.Read();
                    result = (T)_loadMethod.Invoke(null, pars: new object[] { reader.GetString() }, session: _session);
                    reader.Read();
                }
                else
                {
                    result = (T)Activator.CreateInstance(typeof(T));
                    while (reader.TokenType!=JsonTokenType.EndObject)
                    {
                        var prop = typeof(T).GetProperty(reader.GetString());
                        reader.Read();
                        prop.SetValue(result, JsonSerializer.Deserialize(ref reader, prop.PropertyType, options));
                    }
                    reader.Read();
                }
            }
            return result;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value==null)
                writer.WriteNullValue();
            else
            {
                writer.WriteStartObject();

                foreach (var pi in value.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetCustomAttributes(typeof(ModelIgnoreProperty), false).Length == 0 && !p.PropertyType.FullName.Contains("+KeyCollection") && p.GetGetMethod().GetParameters().Length == 0))
                {
                    writer.WritePropertyName(pi.Name);
                    JsonSerializer.Serialize(writer, pi.GetValue(value), pi.PropertyType, options);
                }

                writer.WriteEndObject();
            }
        }
    }
}
