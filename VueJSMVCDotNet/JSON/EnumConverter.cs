﻿using System.Text.Json;
using System.Text.Json.Serialization;

namespace VueJSMVCDotNet.JSON
{
    internal class EnumConverter<T> : JsonConverter<T> where T : Enum
    {
        public EnumConverter()
        {
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType==JsonTokenType.String)
                return (T)Enum.Parse(typeof(T), reader.GetString());
            throw new InvalidCastException();
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value==null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.ToString());
        }
    }
}
