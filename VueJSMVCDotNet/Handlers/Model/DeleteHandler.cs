using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class DeleteHandler : ModelRequestHandlerBase
    {
        private List<IModelActionHandler> _handlers;

        public DeleteHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase,ILogger log)
            :base(next,sessionFactory,registerSlowMethod,urlBase,log)
        {
            _handlers=new List<IModelActionHandler>();
        }

        public override void ClearCache()
        {
            _handlers=new List<IModelActionHandler>();
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            var url = CleanURL(context);
            if (ModelRequestHandlerBase.GetRequestMethod(context)==ModelRequestHandler.RequestMethods.DELETE && _handlers.Any(h=>h.BaseURLs.Contains(url[..url.LastIndexOf("/")],StringComparer.InvariantCultureIgnoreCase)))
            {
                var handler = _handlers.FirstOrDefault(h => h.BaseURLs.Contains(url[..url.LastIndexOf("/")], StringComparer.InvariantCultureIgnoreCase));
                if (handler!=null)
                {
                    await handler.Invoke(url, await ExtractParts(context), context);
                    return;
                }

                throw new CallNotFoundException("Model Not Found");
            }
            else
                await _next(context);
        }

        protected override void InternalLoadTypes(List<Type> types)
        {
            foreach (Type t in types)
            {
                MethodInfo delMethod = t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).FirstOrDefault(m => m.GetCustomAttributes(typeof(ModelDeleteMethod), false).Length>0);
                if (delMethod != null)
                {
                    _handlers.Add((IModelActionHandler)
                        typeof(ModelActionHandler<>).MakeGenericType(new Type[] { t })
                        .GetConstructor(new Type[] { typeof(MethodInfo), typeof(string), typeof(delRegisterSlowMethodInstance), typeof(ILogger) })
                        .Invoke(new object[] { delMethod, "delete", _registerSlowMethod, log })
                    );
                }
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
