﻿using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class StaticMethodHandler : ModelRequestHandlerBase
    {
        private readonly List<IModelActionHandler> _handlers;

        public StaticMethodHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase, ILogger log)
            : base(next, sessionFactory, registerSlowMethod, urlBase, log)
        {
            _handlers=new List<IModelActionHandler>();
        }

        public override void ClearCache()
        {
            _handlers.Clear();
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            string url = CleanURL(context);
            if (ModelRequestHandlerBase.GetRequestMethod(context)==RequestMethods.SMETHOD && _handlers.Any(h=>h.BaseURLs.Contains(url[..url.LastIndexOf("/")], StringComparer.InvariantCultureIgnoreCase) && h.MethodNames.Contains(url[(url.LastIndexOf("/")+1)..], StringComparer.InvariantCultureIgnoreCase)))
            {
                var handler = _handlers.FirstOrDefault(h => h.BaseURLs.Contains(url[..url.LastIndexOf("/")], StringComparer.InvariantCultureIgnoreCase) && h.MethodNames.Contains(url[(url.LastIndexOf("/")+1)..], StringComparer.InvariantCultureIgnoreCase))
                    ??throw new CallNotFoundException("Unable to locate requested method to invoke");
                await handler.InvokeWithoutLoad(url, await ExtractParts(context), context);
                return;
            }
            await _next(context);
        }

        protected override void InternalLoadTypes(List<Type> types){
            foreach (Type t in types)
            {
                foreach (var grp in t.GetMethods(Constants.STATIC_INSTANCE_METHOD_FLAGS)
                    .Where(m => m.GetCustomAttributes(typeof(ExposedMethod), false).Length>0)
                    .GroupBy(m => m.Name))
                {
                    _handlers.Add((IModelActionHandler)
                        typeof(ModelActionHandler<>).MakeGenericType(new Type[] { t })
                        .GetConstructor(new Type[] { typeof(MethodInfo[]), typeof(string), typeof(delRegisterSlowMethodInstance),typeof(ILogger) })
                        .Invoke(new object[] { grp.ToList(), "staticMethod", _registerSlowMethod,log })
                    );
                }
            }
        }

        protected override void InternalUnloadTypes(List<Type> types)
        {
            _handlers.RemoveAll(h =>
                types.Contains(h.GetType().GetGenericArguments()[0])
            );
        }
    }
}
