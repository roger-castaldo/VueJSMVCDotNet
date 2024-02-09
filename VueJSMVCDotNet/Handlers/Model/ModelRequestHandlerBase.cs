using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Interfaces;
using System.Collections;
using System.IO;
using System.Text.Json;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal abstract class ModelRequestHandlerBase
    {
        private const string CONVERTED_URL_KEY = "PARSED_URL";
        private const string REQUEST_DATA_KEY = "CONVERTED_REQUEST_DATA";

        protected readonly RequestDelegate next;
        protected readonly delRegisterSlowMethodInstance registerSlowMethod;
        protected readonly ILogger log;
        private readonly ISecureSessionFactory sessionFactory;
        private readonly string urlBase;

        public ModelRequestHandlerBase(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod,string urlBase, ILogger log)
        {
            this.next = next;
            this.sessionFactory=sessionFactory;
            this.registerSlowMethod=registerSlowMethod;
            this.urlBase=urlBase;
            this.log=log;
        }

        protected async Task<ModelRequestData> ExtractParts(HttpContext context)
        {
            if (!context.Items.ContainsKey(REQUEST_DATA_KEY))
            {
                var session = sessionFactory.ProduceFromContext(context);
                var formData = new Dictionary<string,object>();
                IFormFileCollection files = null;
                if (context.Request.ContentType!=null &&
                (
                    context.Request.ContentType=="application/x-www-form-urlencoded"
                    || context.Request.ContentType.StartsWith("multipart/form-data")
                ))
                {
                    files=context.Request.Form.Files;
                    context.Request.Form.ForEach(pair =>
                    {
                        log?.LogTrace("Loading form data value from key {}", pair.Key);
                        if (pair.Key.EndsWith(":json"))
                        {
                            if (pair.Value.Count>1)
                                formData.Add(pair.Key[..^5], JsonDocument.Parse($"[{string.Join(',', pair.Value)}]"));
                            else
                                formData.Add(pair.Key[..^5], JsonDocument.Parse(pair.Value[0]));
                        }
                        else
                        {
                            if (pair.Value.Count>1)
                                formData.Add(pair.Key, pair.Value.ToList());
                            else
                                formData.Add(pair.Key, pair.Value[0]);
                        }
                    });
                }
                else
                {
                    string tmp = await new StreamReader(context.Request.Body).ReadToEndAsync();
                    if (tmp!="")
                    {
                        log?.LogTrace("Loading form data from request body");
                        JsonDocument.Parse(tmp).RootElement.EnumerateObject()
                            .ForEach(jsonProperty => formData.Add(jsonProperty.Name, jsonProperty.Value));
                    }
                }
                context.Items.Add(REQUEST_DATA_KEY, new ModelRequestData(formData, session,context.RequestServices,context.Features,log,files));
            }
            return (ModelRequestData)context.Items[REQUEST_DATA_KEY];
        }

        protected string CleanURL(HttpContext context)
        {
            if (!context.Items.ContainsKey(CONVERTED_URL_KEY))
                context.Items.Add(CONVERTED_URL_KEY,Utility.CleanURL(Utility.BuildURL(context, urlBase)));
            return (string)context.Items[CONVERTED_URL_KEY];
        }

        protected static RequestMethods GetRequestMethod(HttpContext context)
            => (RequestMethods)Enum.Parse(typeof(RequestMethods), context.Request.Method.ToUpper());

        public void LoadTypes(List<Type> types)
            => InternalLoadTypes(types);
        public void UnloadTypes(List<Type> types)
            => InternalUnloadTypes(types);
        public abstract void ClearCache();
        public abstract Task ProcessRequest(HttpContext context);
        protected abstract void InternalLoadTypes(List<Type> types);
        protected abstract void InternalUnloadTypes(List<Type> types);
    }
}
