using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class UpdateHandler : ModelRequestHandlerBase
    {
        private readonly List<IModelActionHandler> handlers;

        public UpdateHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase, ILogger log) 
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
            if (ModelRequestHandlerBase.GetRequestMethod(context)== ModelRequestHandler.RequestMethods.PATCH 
                && (handler = handlers.FirstOrDefault(h => h.BaseURLs.Contains(url[..url.LastIndexOf("/")], StringComparer.InvariantCultureIgnoreCase)))!=null)
                await handler.Invoke(url, await ExtractParts(context), context, processLoadedModel:(model, data) =>
                {
                    Utility.SetModelValues(data, ref model, false, log);
                    return model;
                });
            else
                await next(context);
        }

        protected override void InternalLoadTypes(List<Type> types)
            => handlers.AddRange(
                types.Select(t => new { type = t, updateMethod = t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).FirstOrDefault(mi => mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0) })
                    .Where(pair => pair.updateMethod!=null)
                    .Select(pair => (IModelActionHandler)
                        typeof(ModelActionHandler<>).MakeGenericType(new Type[] { pair.type })
                        .GetConstructor(new Type[] { typeof(MethodInfo), typeof(string), typeof(delRegisterSlowMethodInstance), typeof(ILogger) })
                        .Invoke(new object[] { pair.updateMethod, "update", registerSlowMethod, log })
                    )
            );

        protected override void InternalUnloadTypes(List<Type> types)
            => handlers.RemoveAll(h =>
                types.Contains(h.GetType().GetGenericArguments()[0])
            );
    }
}
