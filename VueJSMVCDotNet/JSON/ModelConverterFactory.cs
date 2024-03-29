﻿using VueJSMVCDotNet.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VueJSMVCDotNet.JSON
{
    internal class ModelConverterFactory : JsonConverterFactory
    {
        private readonly IRequestData _requestData;
        private readonly ILogger log;

        public ModelConverterFactory(IRequestData requestData,ILogger log)
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
