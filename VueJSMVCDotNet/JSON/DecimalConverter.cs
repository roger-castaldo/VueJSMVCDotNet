using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace VueJSMVCDotNet.JSON
{
    internal class DecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType==JsonTokenType.Number)
            {
                var flt = float.Parse(UTF8Encoding.UTF8.GetString(reader.ValueSpan), NumberStyles.Float);
                if (flt>=(float)decimal.MaxValue)
                    return decimal.MaxValue;
                else if (flt<=(float)decimal.MinValue)
                    return decimal.MinValue;
                return Convert.ToDecimal(flt);
            }
            else if (reader.TokenType==JsonTokenType.String)
                return decimal.Parse(reader.GetString());
            throw new InvalidCastException();
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}
