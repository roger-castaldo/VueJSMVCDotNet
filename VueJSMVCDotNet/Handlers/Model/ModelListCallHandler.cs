using Microsoft.AspNetCore.Http;
using System.Collections;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Base;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.VueMiddleware;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class ModelListCallHandler(ISecureSessionFactory? sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string? urlBase, ILogger? log)
        : ModelActionRequestHandler(sessionFactory, urlBase, log)
    {

        protected override bool CanHandleRequest(HttpContext httpContext, out IModelActionHandler? handler, out string? cacheURL)
        {
            handler=null;
            cacheURL=null;
            if (GetRequestMethod(httpContext)==RequestMethods.LIST)
            {
                var url = CleanURL(httpContext);
                handler=FirstOrDefault(h => h.BaseURLs.Contains(url[..url.LastIndexOf('/')], StringComparer.InvariantCultureIgnoreCase)
                && h.MethodNames.Contains(url[(url.LastIndexOf('/')+1)..], StringComparer.InvariantCultureIgnoreCase));
                cacheURL=url;
            }
            return handler!=null;
        }

        protected override async Task ExecuteActionHandlerAsync(HttpContext context, string url, IModelActionHandler handler)
            => await handler.InvokeWithoutLoad(url, await ExtractParts(context), context, extractResponse: (model, result, opars, method) =>
            {
                if (method.GetCustomAttributes().OfType<ModelListMethodAttribute>().Any(mlm => mlm.Paged))
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
                    Log?.LogTrace("Outputting page information TotalPages:{TotalPages} for {Method}:{URL}", opars[pageIndex], method, Utility.SantizeLogValue(url));
                    return new Hashtable()
                            {
                                {"response",result },
                                {"TotalPages",opars[pageIndex] }
                            };
                }
                return result;
            });

        protected override IEnumerable<IModelActionHandler> GetHandlers(IEnumerable<Type> types)
            => types.SelectMany(t =>
                        t.GetMethods(Constants.STATIC_INSTANCE_METHOD_FLAGS)
                        .Where(m => m.GetCustomAttribute<ModelListMethodAttribute>(false)!=null)
                        .GroupBy(m => m.Name)
                        .Select(grp =>
                            (IModelActionHandler)Activator.CreateInstance(
                                typeof(ModelActionHandler<>).MakeGenericType(t),
                                grp.ToList(),
                                "listMethod",
                                registerSlowMethod,
                                Log
                            )!
                        )
                    );

        protected override void RemoveHandlers(IEnumerable<Type> types, ref List<IModelActionHandler> handlers)
            => handlers.RemoveAll(h =>
                types.Contains(h.GetType().GetGenericArguments()[0])
            );
    }
}
