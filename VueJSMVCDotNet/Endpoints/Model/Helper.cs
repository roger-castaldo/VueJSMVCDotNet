using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Endpoints.DataSources;
using VueJSMVCDotNet.Endpoints.Model.JSGenerators;
using VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Extensions;
using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.Interfaces.Internal;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal static class Helper
    {
        public const string ID_PARAMETER_NAME = "id";
        private const string REQUEST_DATA_KEY = "CONVERTED_REQUEST_DATA";

        public static readonly IEnumerable<IGenerator> JSGenerators = [
                new ParsersGenerator(),
                new ModelClassHeaderGenerator(),
                new JsonGenerator(),
                new ModelDefaultMethodsGenerator(),
                new ParseGenerator(),
                new ModelInstanceFooterGenerator(),
                new ModelLoadAllGenerator(),
                new ModelLoadGenerator(),
                new MethodsGenerator(),
                new EventStreamsGenerator(),
                new ModelListCallGenerator(),
                new ModelClassFooterGenerator(),
                new FooterGenerator()
            ];

        public static async Task<IInternalRequestData> ExtractPartsAsync(HttpContext context, ILogger? logger)
        {
            if (!context.Items.ContainsKey(REQUEST_DATA_KEY))
            {
                ISecureSession? session = await (((ISecureSessionFactory?)context.RequestServices.GetService(typeof(ISecureSessionFactory)))?.ProduceFromContextAsync(context)??Task.FromResult<ISecureSession?>(null));
                var formData = new Dictionary<string, object>();
                IFormFileCollection? files = null;
                JsonDocument? rawBody = null;
                if (context.Request.ContentType != null &&
                (
                    context.Request.ContentType == "application/x-www-form-urlencoded"
                    || context.Request.ContentType.StartsWith("multipart/form-data")
                ))
                {
                    files = context.Request.Form.Files;
                    formData = ProcessRequestForm(context.Request.Form, logger);
                }
                else
                    (rawBody, formData) = await ProcessJsonBody(context.Request.Body, logger);
                context.Items.Add(REQUEST_DATA_KEY,
                    new ModelRequestData(
                        formData,
                        session,
                        context,
                        logger,
                        files,
                        rawBody,
                        context.Request.RouteValues[ID_PARAMETER_NAME]?.ToString(),
                        context.RequestServices.GetRequiredService<ModelsDataSource>()
                    )
                );
            }
            return (ModelRequestData)context.Items[REQUEST_DATA_KEY]!;
        }

        private static async Task<(JsonDocument? rawBody, Dictionary<string, object> formData)> ProcessJsonBody(Stream body, ILogger? logger)
        {
            string tmp = await new StreamReader(body).ReadToEndAsync();
            if (tmp != "")
            {
                logger?.LogTrace("Loading form data from request body");
                var rawBody = JsonDocument.Parse(tmp);
                return (rawBody, new(rawBody.RootElement.EnumerateObject()
                    .Select(jsonProperty => new KeyValuePair<string, object>(jsonProperty.Name, jsonProperty.Value))));
            }
            return (null, []);
        }

        private static Dictionary<string, object> ProcessRequestForm(IFormCollection form, ILogger? logger)
        {
            var result = new Dictionary<string, object>();
            form.ForEach(pair =>
            {
                logger?.LogTrace("Loading form data value from key {Key}", pair.Key);
                if (pair.Key.EndsWith(":json", StringComparison.InvariantCultureIgnoreCase))
                    result.Add(pair.Key[..^5], pair.Value.Count>1 ? JsonDocument.Parse($"[{string.Join(',', pair.Value!)}]") : JsonDocument.Parse(pair.Value[0]!));
                else
                    result.Add(pair.Key, pair.Value.Count>1 ? pair.Value.ToList() : pair.Value[0]!);
            });
            return result;
        }

        public static bool IsExposedMethodInstance(MethodInfo method)
            => Array.Exists(method.GetParameters(), p => p.GetCustomAttribute<ModelIDParameterAttribute>()!=null || p.GetCustomAttribute<ModelInstanceParameterAttribute>()!=null);


    }
}
