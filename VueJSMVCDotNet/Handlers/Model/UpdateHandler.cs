using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Base;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.VueMiddleware;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class UpdateHandler(ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase, ILogger log) 
        : ModelActionRequestHandler(sessionFactory, urlBase, log)
    {
        protected override bool CanHandleRequest(HttpContext httpContext, out IModelActionHandler handler, out string cacheURL)
        {
            handler=null;
            cacheURL=null;
            if (GetRequestMethod(httpContext)== RequestMethods.PATCH)
            {
                var url = CleanURL(httpContext);
                handler = FirstOrDefault(h => h.BaseURLs.Contains(url[..url.LastIndexOf("/")], StringComparer.InvariantCultureIgnoreCase));
                cacheURL=url;
            }
            return handler!=null;
        }

        protected async override Task ExecuteActionHandlerAsync(HttpContext context, string url, IModelActionHandler handler)
            => await handler.Invoke(url, await ExtractParts(context), context, processLoadedModel: (model, data) =>
            {
                Utility.SetModelValues(data, ref model, false, log);
                return model;
            });

        protected override IEnumerable<IModelActionHandler> GetHandlers(IEnumerable<Type> types)
            => types.Select(t => new { type = t, updateMethod = t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).FirstOrDefault(mi => mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0) })
                .Where(pair => pair.updateMethod!=null)
                .Select(pair => (IModelActionHandler)
                    typeof(ModelActionHandler<>).MakeGenericType(new Type[] { pair.type })
                    .GetConstructor(new Type[] { typeof(MethodInfo), typeof(string), typeof(delRegisterSlowMethodInstance), typeof(ILogger) })
                    .Invoke(new object[] { pair.updateMethod, "update", registerSlowMethod, log })
                );

        protected override void RemoveHandlers(IEnumerable<Type> types, ref List<IModelActionHandler> handlers)
            => handlers.RemoveAll(h =>
                    types.Contains(h.GetType().GetGenericArguments()[0])
                );
    }
}
