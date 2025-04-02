using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Text.Json;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Endpoints.DataSources;
using VueJSMVCDotNet.Extensions;
using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.Interfaces.Internal;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal static class Helper
    {
        public const string ID_PARAMETER_NAME = "id";
        private const string REQUEST_DATA_KEY = "CONVERTED_REQUEST_DATA";

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
                    context.Request.Form.ForEach(pair =>
                    {
                        logger?.LogTrace("Loading form data value from key {Key}", pair.Key);
                        if (pair.Key.EndsWith(":json"))
                        {
                            if (pair.Value.Count > 1)
                                formData.Add(pair.Key[..^5], JsonDocument.Parse($"[{string.Join(',', pair.Value!)}]"));
                            else
                                formData.Add(pair.Key[..^5], JsonDocument.Parse(pair.Value[0]!));
                        }
                        else
                        {
                            if (pair.Value.Count > 1)
                                formData.Add(pair.Key, pair.Value.ToList());
                            else
                                formData.Add(pair.Key, pair.Value[0]!);
                        }
                    });
                }
                else
                {
                    string tmp = await new StreamReader(context.Request.Body).ReadToEndAsync();
                    if (tmp != "")
                    {
                        logger?.LogTrace("Loading form data from request body");
                        rawBody = JsonDocument.Parse(tmp);
                        rawBody.RootElement.EnumerateObject()
                            .ForEach(jsonProperty => formData.Add(jsonProperty.Name, jsonProperty.Value));
                    }
                }
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

        public static bool IsExposedMethodInstance(MethodInfo method)
            => Array.Exists(method.GetParameters(), p => p.GetCustomAttribute<ModelIDParameterAttribute>()!=null || p.GetCustomAttribute<ModelInstanceParameterAttribute>()!=null);


    }
}
