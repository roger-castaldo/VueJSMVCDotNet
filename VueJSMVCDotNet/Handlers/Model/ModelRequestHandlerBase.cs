using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model
{
    internal abstract class ModelRequestHandlerBase
    {
        private const string _CONVERTED_URL_KEY = "PARSED_URL";
        private const string _REQUEST_DATA_KEY = "CONVERTED_REQUEST_DATA";

        protected readonly RequestDelegate _next;
        protected readonly delRegisterSlowMethodInstance _registerSlowMethod;
        private readonly ISecureSessionFactory _sessionFactory;
        private readonly string _urlBase;

        public ModelRequestHandlerBase(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod,string urlBase)
        {
            _next = next;
            _sessionFactory=sessionFactory;
            _registerSlowMethod=registerSlowMethod;
            _urlBase=urlBase;
        }

        protected async Task<ModelRequestData> _ExtractParts(HttpContext context)
        {
            if (!context.Items.ContainsKey(_REQUEST_DATA_KEY))
            {
                var session = _sessionFactory.ProduceFromContext(context);
                var formData = new Dictionary<string,object>();
                if (context.Request.ContentType!=null &&
                (
                    context.Request.ContentType=="application/x-www-form-urlencoded"
                    || context.Request.ContentType.StartsWith("multipart/form-data")
                ))
                {
                    foreach (string key in context.Request.Form.Keys)
                    {
                        Logger.Trace("Loading form data value from key {0}", new object[] { key });
                        if (key.EndsWith(":json"))
                        {
                            if (context.Request.Form[key].Count>1)
                            {
                                StringBuilder sb = new StringBuilder();
                                sb.Append("[");
                                foreach (string str in context.Request.Form[key])
                                    sb.Append(str+",");
                                sb.Length--;
                                sb.Append("]");
                                formData.Add(key.Substring(0, key.Length-5), JsonDocument.Parse(sb.ToString()));
                            }
                            else
                            {
                                formData.Add(key.Substring(0, key.Length-5), JsonDocument.Parse(context.Request.Form[key][0]));
                            }
                        }
                        else
                        {
                            if (context.Request.Form[key].Count>1)
                            {
                                ArrayList al = new ArrayList();
                                foreach (string str in context.Request.Form[key])
                                {
                                    al.Add(str);
                                }
                                formData.Add(key, al);
                            }
                            else
                            {
                                formData.Add(key, context.Request.Form[key][0]);
                            }
                        }
                    }
                }
                else
                {
                    string tmp = await new StreamReader(context.Request.Body).ReadToEndAsync();
                    if (tmp!="")
                    {
                        Logger.Trace("Loading form data from request body");
                        foreach (var jsonProperty in JsonDocument.Parse(tmp).RootElement.EnumerateObject())
                            formData.Add(jsonProperty.Name, jsonProperty.Value);
                    }
                }
                context.Items.Add(_REQUEST_DATA_KEY, new ModelRequestData(formData, session,context.Features));
            }
            return (ModelRequestData)context.Items[_REQUEST_DATA_KEY];
        }

        protected string _CleanURL(HttpContext context)
        {
            if (!context.Items.ContainsKey(_CONVERTED_URL_KEY))
                context.Items.Add(_CONVERTED_URL_KEY,Utility.CleanURL(Utility.BuildURL(context, _urlBase)));
            return (string)context.Items[_CONVERTED_URL_KEY];
        }

        protected RequestMethods GetRequestMethod(HttpContext context)
        {
            return (RequestMethods)Enum.Parse(typeof(RequestMethods), context.Request.Method.ToUpper());
        }

        public void LoadTypes(List<Type> types)
        {
            _LoadTypes(types);
        }
        public void UnloadTypes(List<Type> types)
        {
            _UnloadTypes(types);
        }
        public abstract void ClearCache();
        public abstract Task ProcessRequest(HttpContext context);
        protected abstract void _LoadTypes(List<Type> types);
        protected abstract void _UnloadTypes(List<Type> types);
    }
}
