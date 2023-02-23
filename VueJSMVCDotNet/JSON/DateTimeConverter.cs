using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Org.Reddragonit.VueJSMVCDotNet.JSON
{
    internal class DateTimeConverter : JsonConverter<DateTime>
    {
        private const string _DatetimeFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.ParseExact(reader.GetString(), _DatetimeFormat, null, DateTimeStyles.AssumeUniversal);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_DatetimeFormat));
        }
    }
}
