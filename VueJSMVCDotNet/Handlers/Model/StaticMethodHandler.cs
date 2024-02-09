using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class StaticMethodHandler : ModelRequestHandlerBase
    {
        private readonly List<IModelActionHandler> handlers;

        public StaticMethodHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase, ILogger log)
            : base(next, sessionFactory, registerSlowMethod, urlBase, log)
        {
            handlers=new List<IModelActionHandler>();
        }

        public override void ClearCache()
            => handlers.Clear();

        public override async Task ProcessRequest(HttpContext context)
        {
            string url = CleanURL(context);
            IModelActionHandler handler;
            if (ModelRequestHandlerBase.GetRequestMethod(context)==RequestMethods.SMETHOD
                && (handler=handlers.FirstOrDefault(h => h.BaseURLs.Contains(url[..url.LastIndexOf("/")], StringComparer.InvariantCultureIgnoreCase) && h.MethodNames.Contains(url[(url.LastIndexOf("/")+1)..], StringComparer.InvariantCultureIgnoreCase)))!=null)
                await handler.InvokeWithoutLoad(url, await ExtractParts(context), context);
            else
                await next(context);
        }

        protected override void InternalLoadTypes(List<Type> types)
            => handlers.AddRange(
                    types.SelectMany(t => t.GetMethods(Constants.STATIC_INSTANCE_METHOD_FLAGS)
                        .Where(m => m.GetCustomAttributes(typeof(ExposedMethod), false).Length>0)
                        .GroupBy(m => m.Name)
                        .Select(grp => (IModelActionHandler)
                        typeof(ModelActionHandler<>).MakeGenericType(new Type[] { t })
                        .GetConstructor(new Type[] { typeof(MethodInfo[]), typeof(string), typeof(delRegisterSlowMethodInstance), typeof(ILogger) })
                        .Invoke(new object[] { grp.ToList(), "staticMethod", registerSlowMethod, log }))
                    )
                );

        protected override void InternalUnloadTypes(List<Type> types)
            => handlers.RemoveAll(h =>
                types.Contains(h.GetType().GetGenericArguments()[0])
            );
    }
}
