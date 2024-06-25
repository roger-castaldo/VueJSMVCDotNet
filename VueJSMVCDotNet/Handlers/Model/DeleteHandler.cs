using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Base;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.VueMiddleware;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class DeleteHandler(ISecureSessionFactory? sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string? urlBase, ILogger? log)
        : ModelActionRequestHandler(sessionFactory, urlBase, log)
    {
        protected override bool CanHandleRequest(HttpContext httpContext, out IModelActionHandler? handler, out string? cacheURL)
        {
            handler=null;
            cacheURL=null;
            if (GetRequestMethod(httpContext)==RequestMethods.DELETE)
            {
                var url = CleanURL(httpContext);
                handler=FirstOrDefault(h => h.BaseURLs.Contains(url[..url.LastIndexOf("/")], StringComparer.InvariantCultureIgnoreCase));
                cacheURL=url;
            }
            return handler!=null;
        }

        protected override async Task ExecuteActionHandlerAsync(HttpContext context, string url, IModelActionHandler handler)
            => await handler.Invoke(url, await ExtractParts(context), context);

        protected override IEnumerable<IModelActionHandler> GetHandlers(IEnumerable<Type> types)
            => types
                .Select(t => new
                {
                    type = t,
                    delMethod = Array.Find(t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS), m => m.GetCustomAttribute<ModelDeleteMethodAttribute>(false)!=null)
                })
                .Where(pair => pair.delMethod!=null)
                .Select(pair =>
                    (IModelActionHandler)Activator.CreateInstance(
                        typeof(ModelActionHandler<>).MakeGenericType(pair.type),
                        pair.delMethod,
                        "delete",
                        registerSlowMethod,
                        Log
                    )!
                );
    }
}
