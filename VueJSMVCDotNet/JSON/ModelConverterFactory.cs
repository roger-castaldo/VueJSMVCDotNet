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
    internal class ModelConverterFactory : JsonConverterFactory
    {
        private readonly IRequestData _requestData;
        private readonly ILog log;

        public ModelConverterFactory(IRequestData requestData,ILog log)
        {
            _requestData= requestData;
            this.log=log;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.GetInterfaces().Any(iface => iface == typeof(IModel));
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return (JsonConverter)Activator.CreateInstance(typeof(ModelConverter<>).MakeGenericType(new Type[] {typeToConvert}),new object[] {_requestData,log});
        }
    }
}
