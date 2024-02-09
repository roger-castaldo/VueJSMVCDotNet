using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class LoadAllHandler : ModelRequestHandlerBase
    {
        private readonly List<IModelActionHandler> handlers;

        public LoadAllHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase, ILogger log)
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
            if (ModelRequestHandlerBase.GetRequestMethod(context) == ModelRequestHandler.RequestMethods.GET &&
                (handler = handlers.FirstOrDefault(h => h.BaseURLs.Contains(url, StringComparer.InvariantCultureIgnoreCase)))!=null)
                await handler.InvokeWithoutLoad(url, await ExtractParts(context), context);
            else
                await next(context);
        }

        protected override void InternalLoadTypes(List<Type> types)
            => handlers.AddRange(
                types.Select(t => new { type = t, loadAllMethod = t.GetMethods(Constants.LOAD_METHOD_FLAGS).FirstOrDefault(mi => mi.GetCustomAttributes(typeof(ModelLoadAllMethod), false).Length > 0) })
                    .Where(pair=>pair.loadAllMethod!=null)
                    .Select(pair => (IModelActionHandler)
                        typeof(ModelActionHandler<>).MakeGenericType(new Type[] { pair.type })
                        .GetConstructor(new Type[] { typeof(MethodInfo), typeof(string), typeof(delRegisterSlowMethodInstance), typeof(ILogger) })
                        .Invoke(new object[] { pair.loadAllMethod, "loadall", registerSlowMethod, log })
                    )
                );

        protected override void InternalUnloadTypes(List<Type> types)
            => handlers.RemoveAll(h =>
                types.Contains(h.GetType().GetGenericArguments()[0])
            );
    }
}
