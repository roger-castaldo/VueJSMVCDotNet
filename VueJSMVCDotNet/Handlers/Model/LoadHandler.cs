using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Handlers.Base;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.VueMiddleware;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class LoadHandler(ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase, ILogger log) 
        : ModelActionRequestHandler(sessionFactory, urlBase, log)
    {
        protected override bool CanHandleRequest(HttpContext httpContext, out IModelActionHandler handler, out string cacheURL)
        {
            handler=null;
            cacheURL=null;
            if (GetRequestMethod(httpContext) == RequestMethods.GET)
            {
                var url = CleanURL(httpContext);
                handler = FirstOrDefault(h => h.BaseURLs.Contains(url[..url.LastIndexOf("/")], StringComparer.InvariantCultureIgnoreCase));
                cacheURL = url;
            }
            return handler!=null;
        }

        protected override async Task ExecuteActionHandlerAsync(HttpContext context, string url, IModelActionHandler handler)
        {
            var result = await handler.Load(url, await ExtractParts(context));
            context.Response.ContentType = "text/json";
            context.Response.StatusCode= 200;
            await context.Response.WriteAsync(Utility.JsonEncode(result, log));
        }
        protected override IEnumerable<IModelActionHandler> GetHandlers(IEnumerable<Type> types)
            => types.Select(t => (IModelActionHandler)
                    typeof(ModelActionHandler<>).MakeGenericType(new Type[] { t })
                    .GetConstructor(new Type[] { typeof(string), typeof(delRegisterSlowMethodInstance), typeof(ILogger) })
                    .Invoke(new object[] { "load", registerSlowMethod, log })
            );

        protected override void RemoveHandlers(IEnumerable<Type> types, ref List<IModelActionHandler> handlers)
            => handlers.RemoveAll(h =>
                types.Contains(h.GetType().GetGenericArguments()[0])
            );
    }
}
