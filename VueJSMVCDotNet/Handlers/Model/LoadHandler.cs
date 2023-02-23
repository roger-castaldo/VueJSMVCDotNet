using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.ModelRequestHandler;
using static System.Collections.Specialized.BitVector32;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model
{
    internal class LoadHandler : ModelRequestHandlerBase
    {
        private readonly List<IModelActionHandler> _handlers;

        public LoadHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase)
            :base(next,sessionFactory,registerSlowMethod,urlBase)
        {
            _handlers=new List<IModelActionHandler>();
        }

        public override void ClearCache()
        {
            _handlers.Clear();
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            string url = _CleanURL(context);
            if (GetRequestMethod(context) == ModelRequestHandler.RequestMethods.GET && _handlers.Any(h => h.BaseURLs.Contains(url.Substring(0, url.LastIndexOf("/")), StringComparer.InvariantCultureIgnoreCase)))
            {
                var handler = _handlers.FirstOrDefault(h => h.BaseURLs.Contains(url.Substring(0, url.LastIndexOf("/")), StringComparer.InvariantCultureIgnoreCase));
                if (handler==null)
                    throw new CallNotFoundException("Model Not Found");
                var result = handler.Load(url, await _ExtractParts(context));
                context.Response.ContentType = "text/json";
                context.Response.StatusCode= 200;
                await context.Response.WriteAsync(Utility.JsonEncode(result));
                return;
            }
            await _next(context);
        }

        protected override void _LoadTypes(List<Type> types)
        {
            foreach (Type t in types)
            {
                _handlers.Add((IModelActionHandler)
                    typeof(ModelActionHandler<>).MakeGenericType(new Type[] { t })
                    .GetConstructor(new Type[] { typeof(string), typeof(delRegisterSlowMethodInstance) })
                    .Invoke(new object[] { "load", _registerSlowMethod })
                );
            }
        }

        protected override void _UnloadTypes(List<Type> types)
        {
            _handlers.RemoveAll(h =>
                types.Contains(h.GetType().GetGenericArguments()[0])
            );
        }
    }
}
