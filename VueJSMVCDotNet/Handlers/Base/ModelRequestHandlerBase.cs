using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using System.IO;
using System.Text.Json;
using VueJSMVCDotNet.Handlers.Model;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Handlers.Base
{
    internal abstract class ModelRequestHandlerBase(ISecureSessionFactory sessionFactory, 
        string urlBase, ILogger log) : RequestHandler(), ITypeSensitiveHandler
    {
        private const string CONVERTED_URL_KEY = "PARSED_URL";
        private const string REQUEST_DATA_KEY = "CONVERTED_REQUEST_DATA";

        internal enum RequestMethods
        {
            GET,
            PUT,
            DELETE,
            PATCH,
            METHOD,
            SMETHOD,
            PULL,
            LIST
        }


        protected async Task<ModelRequestData> ExtractParts(HttpContext context)
        {
            if (!context.Items.ContainsKey(REQUEST_DATA_KEY))
            {
                var session = await sessionFactory.ProduceFromContextAsync(context);
                var formData = new Dictionary<string, object>();
                IFormFileCollection files = null;
                if (context.Request.ContentType != null &&
                (
                    context.Request.ContentType == "application/x-www-form-urlencoded"
                    || context.Request.ContentType.StartsWith("multipart/form-data")
                ))
                {
                    files = context.Request.Form.Files;
                    context.Request.Form.ForEach(pair =>
                    {
                        log?.LogTrace("Loading form data value from key {}", pair.Key);
                        if (pair.Key.EndsWith(":json"))
                        {
                            if (pair.Value.Count > 1)
                                formData.Add(pair.Key[..^5], JsonDocument.Parse($"[{string.Join(',', pair.Value)}]"));
                            else
                                formData.Add(pair.Key[..^5], JsonDocument.Parse(pair.Value[0]));
                        }
                        else
                        {
                            if (pair.Value.Count > 1)
                                formData.Add(pair.Key, pair.Value.ToList());
                            else
                                formData.Add(pair.Key, pair.Value[0]);
                        }
                    });
                }
                else
                {
                    string tmp = await new StreamReader(context.Request.Body).ReadToEndAsync();
                    if (tmp != "")
                    {
                        log?.LogTrace("Loading form data from request body");
                        JsonDocument.Parse(tmp).RootElement.EnumerateObject()
                            .ForEach(jsonProperty => formData.Add(jsonProperty.Name, jsonProperty.Value));
                    }
                }
                context.Items.Add(REQUEST_DATA_KEY, new ModelRequestData(formData, session, context, log, files));
            }
            return (ModelRequestData)context.Items[REQUEST_DATA_KEY];
        }

        protected string CleanURL(HttpContext context)
        {
            if (!context.Items.ContainsKey(CONVERTED_URL_KEY))
                context.Items.Add(CONVERTED_URL_KEY, Utility.CleanURL(Utility.BuildURL(context, urlBase)));
            return (string)context.Items[CONVERTED_URL_KEY];
        }

        protected static RequestMethods GetRequestMethod(HttpContext context)
            => (RequestMethods)Enum.Parse(typeof(RequestMethods), context.Request.Method.ToUpper());

        public abstract void LoadTypes(IEnumerable<Type> types);
        public abstract void UnloadTypes(IEnumerable<Type> types);
        public abstract void ClearTypes();
    }
}
