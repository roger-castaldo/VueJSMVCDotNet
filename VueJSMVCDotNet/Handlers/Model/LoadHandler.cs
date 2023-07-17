using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class LoadHandler : ModelRequestHandlerBase
    {
        private readonly List<IModelActionHandler> _handlers;

        public LoadHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase, ILogger log) 
            : base(next, sessionFactory, registerSlowMethod, urlBase, log)
        {
            _handlers=new List<IModelActionHandler>();
        }

        public override void ClearCache()
        {
            _handlers.Clear();
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            string url = CleanURL(context);
            if (ModelRequestHandlerBase.GetRequestMethod(context) == ModelRequestHandler.RequestMethods.GET && _handlers.Any(h => h.BaseURLs.Contains(url[..url.LastIndexOf("/")], StringComparer.InvariantCultureIgnoreCase)))
            {
                var handler = _handlers.FirstOrDefault(h => h.BaseURLs.Contains(url[..url.LastIndexOf("/")], StringComparer.InvariantCultureIgnoreCase))
                    ??throw new CallNotFoundException("Model Not Found");
                var result = handler.Load(url, await ExtractParts(context));
                context.Response.ContentType = "text/json";
                context.Response.StatusCode= 200;
                await context.Response.WriteAsync(Utility.JsonEncode(result, log));
                return;
            }
            await _next(context);
        }

        protected override void InternalLoadTypes(List<Type> types)
        {
            foreach (Type t in types)
            {
                _handlers.Add((IModelActionHandler)
                    typeof(ModelActionHandler<>).MakeGenericType(new Type[] { t })
                    .GetConstructor(new Type[] { typeof(string), typeof(delRegisterSlowMethodInstance),typeof(ILogger) })
                    .Invoke(new object[] { "load", _registerSlowMethod,log })
                );
            }
        }

        protected override void InternalUnloadTypes(List<Type> types)
        {
            _handlers.RemoveAll(h =>
                types.Contains(h.GetType().GetGenericArguments()[0])
            );
        }
    }
}
