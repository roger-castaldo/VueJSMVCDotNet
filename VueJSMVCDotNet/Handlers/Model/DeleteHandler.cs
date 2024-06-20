using Microsoft.AspNetCore.Http;
using System.Threading;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Base;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.VueMiddleware;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class DeleteHandler(ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase, ILogger log) 
        : ModelActionRequestHandler(sessionFactory,urlBase,log)
    {
        protected override bool CanHandleRequest(HttpContext httpContext, out IModelActionHandler handler, out string cacheURL)
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
        {
            await handler.Invoke(url, await ExtractParts(context), context);
        }

        protected override IEnumerable<IModelActionHandler> GetHandlers(IEnumerable<Type> types)
            => types.Select(t => new { type = t, delMethod = t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).FirstOrDefault(m => m.GetCustomAttributes(typeof(ModelDeleteMethod), false).Length>0) })
                    .Where(pair => pair.delMethod!=null)
                    .Select(pair => (IModelActionHandler)
                        typeof(ModelActionHandler<>).MakeGenericType(new Type[] { pair.type })
                        .GetConstructor(new Type[] { typeof(MethodInfo), typeof(string), typeof(delRegisterSlowMethodInstance), typeof(ILogger) })
                        .Invoke(new object[] { pair.delMethod, "delete", registerSlowMethod, log })
                    );
    }
}
