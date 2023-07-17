using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;
using static System.Collections.Specialized.BitVector32;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class InstanceMethodHandler : ModelRequestHandlerBase
    {
        private readonly List<IModelActionHandler> _handlers;

        public InstanceMethodHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase, ILogger log)
            :base(next,sessionFactory, registerSlowMethod, urlBase, log)
        {
            _handlers=new List<IModelActionHandler>();
        }

        public override void ClearCache()
        {
            _handlers.Clear();
        }

        private static readonly Regex _regUrlSplit = new("^(.+)/([^/]+)/([^/]+)$", RegexOptions.Compiled|RegexOptions.ECMAScript,TimeSpan.FromMilliseconds(500));

        public override async Task ProcessRequest(HttpContext context)
        {
            string url = CleanURL(context);
            var match = _regUrlSplit.Match(url);
            if (ModelRequestHandlerBase.GetRequestMethod(context)==ModelRequestHandler.RequestMethods.METHOD && match.Success && _handlers.Any(h => h.BaseURLs.Contains(match.Groups[1].Value, StringComparer.InvariantCultureIgnoreCase) && h.MethodNames.Contains(match.Groups[3].Value, StringComparer.InvariantCultureIgnoreCase)))
            {
                var handler = _handlers.FirstOrDefault(h => h.BaseURLs.Contains(match.Groups[1].Value, StringComparer.InvariantCultureIgnoreCase) && h.MethodNames.Contains(match.Groups[3].Value, StringComparer.InvariantCultureIgnoreCase));
                if (handler!=null)
                {
                    await handler.Invoke(url, await ExtractParts(context), context, extractID: (url) =>
                                {
                                    return _regUrlSplit.Match(url).Groups[2].Value;
                                });
                    return;
                }

                throw new CallNotFoundException("Unable to locate requested method to invoke");
            }
            else
                await _next(context);
        }
        protected override void InternalLoadTypes(List<Type> types){
            foreach (Type t in types)
            {
                foreach (var grp in t.GetMethods(Constants.INSTANCE_METHOD_FLAGS)
                    .Where(m => m.GetCustomAttributes(typeof(ExposedMethod), false).Length>0)
                    .GroupBy(m => m.Name))
                {
                    _handlers.Add((IModelActionHandler)
                        typeof(ModelActionHandler<>).MakeGenericType(new Type[] { t })
                        .GetConstructor(new Type[] { typeof(MethodInfo[]), typeof(string), typeof(delRegisterSlowMethodInstance), typeof(ILogger) })
                        .Invoke(new object[] { grp.ToList(), "instanceMethod", _registerSlowMethod, log })
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
