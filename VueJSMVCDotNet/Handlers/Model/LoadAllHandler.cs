using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Base;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.VueMiddleware;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class LoadAllHandler(ISecureSessionFactory? sessionFactory, delRegisterSlowMethodInstance registerSlowMethod,
        string? urlBase, ILogger? log) :
        ModelActionRequestHandler(sessionFactory, urlBase, log)
    {
        protected override bool CanHandleRequest(HttpContext httpContext, out IModelActionHandler? handler, out string? cacheURL)
        {
            handler=null;
            cacheURL=null;
            if (GetRequestMethod(httpContext) == RequestMethods.GET)
            {
                var url = CleanURL(httpContext);
                handler = FirstOrDefault(h => h.BaseURLs.Contains(url, StringComparer.InvariantCultureIgnoreCase));
                cacheURL=url;
            }
            return handler!=null;
        }

        protected override async Task ExecuteActionHandlerAsync(HttpContext context, string url, IModelActionHandler handler)
            => await handler.InvokeWithoutLoad(url, await ExtractParts(context), context);

        protected override IEnumerable<IModelActionHandler> GetHandlers(IEnumerable<Type> types)
        => types
            .Select(t => new
            {
                type = t,
                loadAllMethod = Array.Find(t.GetMethods(Constants.LOAD_METHOD_FLAGS), mi => mi.GetCustomAttribute<ModelLoadAllMethodAttribute>(false)!=null)
            })
            .Where(pair => pair.loadAllMethod!=null)
            .Select(pair =>
                (IModelActionHandler)Activator.CreateInstance(
                    typeof(ModelActionHandler<>).MakeGenericType(pair.type),
                    pair.loadAllMethod,
                    "loadall",
                    registerSlowMethod,
                    Log
                )!
            );
    }
}
