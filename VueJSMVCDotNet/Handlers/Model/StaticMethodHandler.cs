﻿using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.ModelRequestHandler;
using static System.Collections.Specialized.BitVector32;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model
{
    internal class StaticMethodHandler : ModelRequestHandlerBase
    {
        private readonly List<IModelActionHandler> _handlers;

        public StaticMethodHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase)
            : base(next,sessionFactory, registerSlowMethod, urlBase)
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
            if (GetRequestMethod(context)==RequestMethods.SMETHOD && _handlers.Any(h=>h.BaseURLs.Contains(url.Substring(0,url.LastIndexOf("/")), StringComparer.InvariantCultureIgnoreCase) && h.MethodNames.Contains(url.Substring(url.LastIndexOf("/")+1), StringComparer.InvariantCultureIgnoreCase)))
            {
                var handler = _handlers.FirstOrDefault(h => h.BaseURLs.Contains(url.Substring(0, url.LastIndexOf("/")), StringComparer.InvariantCultureIgnoreCase) && h.MethodNames.Contains(url.Substring(url.LastIndexOf("/")+1), StringComparer.InvariantCultureIgnoreCase));
                if (handler==null)
                    throw new CallNotFoundException("Unable to locate requested method to invoke");
                await handler.InvokeWithoutLoad(url, await _ExtractParts(context), context);
                return;
            }
            await _next(context);
        }

        protected override void _LoadTypes(List<Type> types){
            foreach (Type t in types)
            {
                foreach (var grp in t.GetMethods(Constants.STATIC_INSTANCE_METHOD_FLAGS)
                    .Where(m => m.GetCustomAttributes(typeof(ExposedMethod), false).Length>0)
                    .GroupBy(m => m.Name))
                {
                    _handlers.Add((IModelActionHandler)
                        typeof(ModelActionHandler<>).MakeGenericType(new Type[] { t })
                        .GetConstructor(new Type[] { typeof(MethodInfo[]), typeof(string), typeof(delRegisterSlowMethodInstance) })
                        .Invoke(new object[] { grp.ToList(), "staticMethod", _registerSlowMethod })
                    );
                }
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
