using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class LoadAllHandler : ModelRequestHandlerBase
    {
        private readonly List<IModelActionHandler> _handlers;

        public LoadAllHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase, ILogger log) 
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
            if (ModelRequestHandlerBase.GetRequestMethod(context) == ModelRequestHandler.RequestMethods.GET && _handlers.Any(h => h.BaseURLs.Contains(url, StringComparer.InvariantCultureIgnoreCase)))
            {
                var handler = _handlers.FirstOrDefault(h => h.BaseURLs.Contains(url, StringComparer.InvariantCultureIgnoreCase))
                    ??throw new CallNotFoundException("Model Not Found");
                await handler.InvokeWithoutLoad(url, await ExtractParts(context), context);
                return;
            }
            await _next(context);
        }

        protected override void InternalLoadTypes(List<Type> types)
        {
            foreach (Type t in types)
            {
                MethodInfo loadAllMethod = t.GetMethods(Constants.LOAD_METHOD_FLAGS).FirstOrDefault(mi => mi.GetCustomAttributes(typeof(ModelLoadAllMethod), false).Length > 0);
                if (loadAllMethod != null)
                {
                    _handlers.Add((IModelActionHandler)
                        typeof(ModelActionHandler<>).MakeGenericType(new Type[] { t })
                        .GetConstructor(new Type[] { typeof(MethodInfo), typeof(string), typeof(delRegisterSlowMethodInstance),typeof(ILogger) })
                        .Invoke(new object[] { loadAllMethod, "loadall", _registerSlowMethod,log })
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
