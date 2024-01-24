using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using System.Collections;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class ModelListCallHandler : ModelRequestHandlerBase
    {
        private readonly List<IModelActionHandler> _handlers;

        public ModelListCallHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase, ILogger log)
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
            string url = CleanURL(context);
            log?.LogTrace("Checking to see if {}:{} is handled by the model list call",ModelRequestHandlerBase.GetRequestMethod(context), url);
            if (ModelRequestHandlerBase.GetRequestMethod(context)==ModelRequestHandler.RequestMethods.LIST && _handlers.Any(h => h.BaseURLs.Contains(url[..url.LastIndexOf("/")], StringComparer.InvariantCultureIgnoreCase) && h.MethodNames.Contains(url[(url.LastIndexOf("/")+1)..], StringComparer.InvariantCultureIgnoreCase)))
            {
                var handler = _handlers.FirstOrDefault(h => h.BaseURLs.Contains(url[..url.LastIndexOf("/")], StringComparer.InvariantCultureIgnoreCase) && h.MethodNames.Contains(url[(url.LastIndexOf("/") + 1)..], StringComparer.InvariantCultureIgnoreCase))
                    ??throw new CallNotFoundException("Unable to locate requested method to invoke");
                await handler.InvokeWithoutLoad(url, await ExtractParts(context), context, extractResponse: (model, result, opars,method) =>
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
                        log?.LogTrace("Outputting page information TotalPages:{} for {}:{}", opars[pageIndex], method, Utility.SantizeLogValue(url));
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

        protected override void InternalLoadTypes(List<Type> types)
        {
            foreach (Type t in types)
            {
                foreach (var grp in t.GetMethods(Constants.STATIC_INSTANCE_METHOD_FLAGS)
                    .Where(m => m.GetCustomAttributes(typeof(ModelListMethod), false).Length>0)
                    .GroupBy(m => m.Name))
                {
                    _handlers.Add((IModelActionHandler)
                        typeof(ModelActionHandler<>).MakeGenericType(new Type[] { t })
                        .GetConstructor(new Type[] { typeof(MethodInfo[]), typeof(string), typeof(delRegisterSlowMethodInstance),typeof(ILogger) })
                        .Invoke(new object[] { grp.ToList(), "listMethod", _registerSlowMethod,log })
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
