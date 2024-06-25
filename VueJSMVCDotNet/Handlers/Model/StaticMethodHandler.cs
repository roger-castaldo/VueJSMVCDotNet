using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Base;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.VueMiddleware;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class StaticMethodHandler(ISecureSessionFactory? sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string? urlBase, ILogger? log)
        : ModelActionRequestHandler(sessionFactory, urlBase, log)
    {
        protected override bool CanHandleRequest(HttpContext httpContext, out IModelActionHandler? handler, out string? cacheURL)
        {
            handler=null;
            cacheURL=null;
            if (ModelRequestHandlerBase.GetRequestMethod(httpContext)==RequestMethods.SMETHOD)
            {
                var url = CleanURL(httpContext);
                handler = FirstOrDefault(h => h.BaseURLs.Contains(url[..url.LastIndexOf("/")], StringComparer.InvariantCultureIgnoreCase) && h.MethodNames.Contains(url[(url.LastIndexOf("/")+1)..], StringComparer.InvariantCultureIgnoreCase));
                cacheURL=url;
            }
            return handler!=null;
        }

        protected async override Task ExecuteActionHandlerAsync(HttpContext context, string url, IModelActionHandler handler)
            => await handler.InvokeWithoutLoad(url, await ExtractParts(context), context);

        protected override IEnumerable<IModelActionHandler> GetHandlers(IEnumerable<Type> types)
            => types
                .SelectMany(t =>
                    t.GetMethods(Constants.STATIC_INSTANCE_METHOD_FLAGS)
                        .Where(m => m.GetCustomAttributes<ExposedMethodAttribute>(false)!=null)
                        .GroupBy(m => m.Name)
                        .Select(grp =>
                            (IModelActionHandler)Activator.CreateInstance(
                                typeof(ModelActionHandler<>).MakeGenericType(t),
                                grp.ToList(),
                                "staticMethod",
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
