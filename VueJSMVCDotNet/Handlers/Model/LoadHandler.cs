using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class LoadHandler : ModelRequestHandlerBase
    {
        private readonly List<IModelActionHandler> handlers;

        public LoadHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase, ILogger log) 
            : base(next, sessionFactory, registerSlowMethod, urlBase, log)
        {
            handlers=new List<IModelActionHandler>();
        }

        public override void ClearCache()
            => handlers.Clear();

        public override async Task ProcessRequest(HttpContext context)
        {
            string url = CleanURL(context);
            IModelActionHandler handler = null;
            if (ModelRequestHandlerBase.GetRequestMethod(context) == ModelRequestHandler.RequestMethods.GET 
                && (handler=handlers.FirstOrDefault(h => h.BaseURLs.Contains(url[..url.LastIndexOf("/")], StringComparer.InvariantCultureIgnoreCase)))!=null)
            {
                var result = handler.Load(url, await ExtractParts(context));
                context.Response.ContentType = "text/json";
                context.Response.StatusCode= 200;
                await context.Response.WriteAsync(Utility.JsonEncode(result, log));
            }else
                await next(context);
        }

        protected override void InternalLoadTypes(List<Type> types)
            => handlers.AddRange(
                    types.Select(t=> (IModelActionHandler)
                    typeof(ModelActionHandler<>).MakeGenericType(new Type[] { t })
                    .GetConstructor(new Type[] { typeof(string), typeof(delRegisterSlowMethodInstance), typeof(ILogger) })
                    .Invoke(new object[] { "load", registerSlowMethod, log }))
                );
        
        protected override void InternalUnloadTypes(List<Type> types)
            => handlers.RemoveAll(h =>
                types.Contains(h.GetType().GetGenericArguments()[0])
            );
    }
}
