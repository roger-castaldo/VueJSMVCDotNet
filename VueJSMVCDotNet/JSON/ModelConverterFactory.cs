using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Org.Reddragonit.VueJSMVCDotNet.JSON
{
    internal class ModelConverterFactory : JsonConverterFactory
    {
        private readonly IRequestData _requestData;

        public ModelConverterFactory(IRequestData requestData)
        {
            _requestData= requestData;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.GetInterfaces().Any(iface => iface == typeof(IModel));
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return (JsonConverter)Activator.CreateInstance(typeof(ModelConverter<>).MakeGenericType(new Type[] {typeToConvert}),new object[] {_requestData});
        }
    }
}
