using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using System.Collections;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class SaveHandler : ModelRequestHandlerBase
    {
        private readonly List<IModelActionHandler> handlers;

        public SaveHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase,ILogger log)
            :base(next,sessionFactory,registerSlowMethod,urlBase,log)
        {
            handlers=new List<IModelActionHandler>();
        }

        public override void ClearCache()
            => handlers.Clear();

        public override async Task ProcessRequest(HttpContext context)
        {
            string url = CleanURL(context);
            log?.LogTrace("Checking to see if {}:{} is handled by the Save Handler", ModelRequestHandlerBase.GetRequestMethod(context), url);
            IModelActionHandler handler = null;
            if (ModelRequestHandlerBase.GetRequestMethod(context)==ModelRequestHandler.RequestMethods.PUT 
                && (handler=handlers.FirstOrDefault(h => h.BaseURLs.Contains(url, StringComparer.InvariantCultureIgnoreCase)))!=null)
            {
                ModelRequestData requestData = await ExtractParts(context);
                var model = (IModel)Activator.CreateInstance(handler.GetType().GetGenericArguments()[0]);
                Utility.SetModelValues(requestData, ref model, true,log);
                await handler.InvokeWithoutLoad(url, requestData, context, model, extractResponse: (model, response,pars,method) =>
                {
                    if ((bool)response)
                        return new Hashtable() { {"id", model.id }};
                    throw new SaveFailedException(model.GetType(), method);
                });
            }else
                await next(context);
        }

       protected override void InternalLoadTypes(List<Type> types)
            => handlers.AddRange(
                types.Select(t => new { type = t, saveMethod = t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).FirstOrDefault(m => m.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0) })
                    .Where(pair => pair.saveMethod!=null)
                    .Select(pair => (IModelActionHandler)
                        typeof(ModelActionHandler<>).MakeGenericType(new Type[] { pair.type })
                        .GetConstructor(new Type[] { typeof(MethodInfo), typeof(string), typeof(delRegisterSlowMethodInstance), typeof(ILogger) })
                        .Invoke(new object[] { pair.saveMethod, "save", registerSlowMethod, log })
                    )
            );

        protected override void InternalUnloadTypes(List<Type> types)
            => handlers.RemoveAll(h =>
                types.Contains(h.GetType().GetGenericArguments()[0])
            );
    }
}
