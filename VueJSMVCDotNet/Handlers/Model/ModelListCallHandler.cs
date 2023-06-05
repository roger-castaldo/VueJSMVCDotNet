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
    internal class ModelListCallHandler : ModelRequestHandlerBase
    {
        private readonly List<IModelActionHandler> _handlers;

        public ModelListCallHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase, ILog log)
            :base(next,sessionFactory,registerSlowMethod,urlBase,log)
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
            log.Trace("Checking to see if {0}:{1} is handled by the model list call", new object[] { GetRequestMethod(context), url });
            if (GetRequestMethod(context)==ModelRequestHandler.RequestMethods.LIST && _handlers.Any(h => h.BaseURLs.Contains(url.Substring(0, url.LastIndexOf("/")), StringComparer.InvariantCultureIgnoreCase) && h.MethodNames.Contains(url.Substring(url.LastIndexOf("/")+1), StringComparer.InvariantCultureIgnoreCase)))
            {
                var handler = _handlers.FirstOrDefault(h => h.BaseURLs.Contains(url.Substring(0, url.LastIndexOf("/")), StringComparer.InvariantCultureIgnoreCase) && h.MethodNames.Contains(url.Substring(url.LastIndexOf("/")+1), StringComparer.InvariantCultureIgnoreCase));
                if (handler==null)
                    throw new CallNotFoundException("Unable to locate requested method to invoke");
                await handler.InvokeWithoutLoad(url, await _ExtractParts(context), context, extractResponse: (model, result, opars,method) =>
                {
                    if (method.GetCustomAttributes().OfType<ModelListMethod>().Any(mlm => mlm.Paged))
                    {
                        var pars = method.StrippedParameters;
                        int pageIndex = opars.Length-1;
                        for (int x = 0; x<pars.Length; x++)
                        {
                            if (pars[x].IsOut)
                            {
                                pageIndex=x;
                                break;
                            }
                        }
                        log.Trace("Outputting page information TotalPages:{0} for {1}:{2}", new object[] { opars[pageIndex], method, url });
                        return new Hashtable()
                        {
                            {"response",result },
                            {"TotalPages",opars[pageIndex] }
                        };
                    }
                    return result;
                });
                return;
            }
            await _next(context);
        }

        protected override void _LoadTypes(List<Type> types)
        {
            foreach (Type t in types)
            {
                foreach (var grp in t.GetMethods(Constants.STATIC_INSTANCE_METHOD_FLAGS)
                    .Where(m => m.GetCustomAttributes(typeof(ModelListMethod), false).Length>0)
                    .GroupBy(m => m.Name))
                {
                    _handlers.Add((IModelActionHandler)
                        typeof(ModelActionHandler<>).MakeGenericType(new Type[] { t })
                        .GetConstructor(new Type[] { typeof(MethodInfo[]), typeof(string), typeof(delRegisterSlowMethodInstance),typeof(ILog) })
                        .Invoke(new object[] { grp.ToList(), "listMethod", _registerSlowMethod,log })
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
