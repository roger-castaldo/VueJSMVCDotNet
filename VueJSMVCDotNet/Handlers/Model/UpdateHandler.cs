using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class UpdateHandler : ModelRequestHandlerBase
    {
        private readonly List<IModelActionHandler> _handlers;

        public UpdateHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase, ILogger log) 
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
            if (ModelRequestHandlerBase.GetRequestMethod(context)== ModelRequestHandler.RequestMethods.PATCH && _handlers.Any(h => h.BaseURLs.Contains(url[..url.LastIndexOf("/")], StringComparer.InvariantCultureIgnoreCase)))
            {
                var handler = _handlers.FirstOrDefault(h => h.BaseURLs.Contains(url[..url.LastIndexOf("/")], StringComparer.InvariantCultureIgnoreCase))
                    ??throw new CallNotFoundException("Model Not Found");
                await handler.Invoke(url, await ExtractParts(context), context, processLoadedModel:(model, data) =>
                {
                    Utility.SetModelValues(data, ref model, false, log);
                    return model;
                });
                return;
            }
            await _next(context);
        }

        protected override void InternalLoadTypes(List<Type> types){
            foreach (Type t in types)
            {
                MethodInfo updateMethod = t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).FirstOrDefault(mi => mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0);
                if (updateMethod != null)
                {
                    _handlers.Add((IModelActionHandler)
                        typeof(ModelActionHandler<>).MakeGenericType(new Type[] { t })
                        .GetConstructor(new Type[] { typeof(MethodInfo), typeof(string), typeof(delRegisterSlowMethodInstance), typeof(ILogger) })
                        .Invoke(new object[] { updateMethod, "update", _registerSlowMethod, log })
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
