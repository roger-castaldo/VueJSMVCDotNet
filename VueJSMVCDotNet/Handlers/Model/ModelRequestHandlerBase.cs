using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Interfaces;
using System.Collections;
using System.IO;
using System.Text.Json;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal abstract class ModelRequestHandlerBase
    {
        private const string _CONVERTED_URL_KEY = "PARSED_URL";
        private const string _REQUEST_DATA_KEY = "CONVERTED_REQUEST_DATA";

        protected readonly RequestDelegate _next;
        protected readonly delRegisterSlowMethodInstance _registerSlowMethod;
        protected readonly ILogger log;
        private readonly ISecureSessionFactory _sessionFactory;
        private readonly string _urlBase;

        public ModelRequestHandlerBase(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod,string urlBase, ILogger log)
        {
            _next = next;
            _sessionFactory=sessionFactory;
            _registerSlowMethod=registerSlowMethod;
            _urlBase=urlBase;
            this.log=log;
        }

        protected async Task<ModelRequestData> ExtractParts(HttpContext context)
        {
            if (!context.Items.ContainsKey(_REQUEST_DATA_KEY))
            {
                var session = _sessionFactory.ProduceFromContext(context);
                var formData = new Dictionary<string,object>();
                IFormFileCollection files = null;
                if (context.Request.ContentType!=null &&
                (
                    context.Request.ContentType=="application/x-www-form-urlencoded"
                    || context.Request.ContentType.StartsWith("multipart/form-data")
                ))
                {
                    files=context.Request.Form.Files;
                    foreach (string key in context.Request.Form.Keys)
                    {
                        log?.LogTrace("Loading form data value from key {}", key);
                        if (key.EndsWith(":json"))
                        {
                            if (context.Request.Form[key].Count>1)
                            {
                                StringBuilder sb = new();
                                sb.Append('[');
                                foreach (string str in context.Request.Form[key])
                                    sb.Append($"{str},");
                                sb.Length--;
                                sb.Append(']');
                                formData.Add(key[..^5], JsonDocument.Parse(sb.ToString()));
                            }
                            else
                                formData.Add(key[..^5], JsonDocument.Parse(context.Request.Form[key][0]));
                        }
                        else
                        {
                            if (context.Request.Form[key].Count>1)
                            {
                                ArrayList al = new();
                                foreach (string str in context.Request.Form[key])
                                    al.Add(str);
                                formData.Add(key, al);
                            }
                            else
                                formData.Add(key, context.Request.Form[key][0]);
                        }
                    }
                }
                else
                {
                    string tmp = await new StreamReader(context.Request.Body).ReadToEndAsync();
                    if (tmp!="")
                    {
                        log?.LogTrace("Loading form data from request body");
                        foreach (var jsonProperty in JsonDocument.Parse(tmp).RootElement.EnumerateObject())
                            formData.Add(jsonProperty.Name, jsonProperty.Value);
                    }
                }
                context.Items.Add(_REQUEST_DATA_KEY, new ModelRequestData(formData, session,context.RequestServices,context.Features,log,files));
            }
            return (ModelRequestData)context.Items[_REQUEST_DATA_KEY];
        }

        protected string CleanURL(HttpContext context)
        {
            if (!context.Items.ContainsKey(_CONVERTED_URL_KEY))
                context.Items.Add(_CONVERTED_URL_KEY,Utility.CleanURL(Utility.BuildURL(context, _urlBase)));
            return (string)context.Items[_CONVERTED_URL_KEY];
        }

        protected static RequestMethods GetRequestMethod(HttpContext context)
        {
            return (RequestMethods)Enum.Parse(typeof(RequestMethods), context.Request.Method.ToUpper());
        }

        public void LoadTypes(List<Type> types)
        {
            InternalLoadTypes(types);
        }
        public void UnloadTypes(List<Type> types)
        {
            InternalUnloadTypes(types);
        }
        public abstract void ClearCache();
        public abstract Task ProcessRequest(HttpContext context);
        protected abstract void InternalLoadTypes(List<Type> types);
        protected abstract void InternalUnloadTypes(List<Type> types);
    }
}
