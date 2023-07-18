using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
