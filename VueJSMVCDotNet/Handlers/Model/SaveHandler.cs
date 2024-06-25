using Microsoft.AspNetCore.Http;
using System.Collections;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Base;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.VueMiddleware;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class SaveHandler(ISecureSessionFactory? sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string? urlBase, ILogger? log)
        : ModelActionRequestHandler(sessionFactory, urlBase, log)
    {
        protected override bool CanHandleRequest(HttpContext httpContext, out IModelActionHandler? handler, out string? cacheURL)
        {
            handler=null;
            cacheURL=null;
            if (GetRequestMethod(httpContext)==RequestMethods.PUT)
            {
                var url = CleanURL(httpContext);
                handler = FirstOrDefault(h => h.BaseURLs.Contains(url, StringComparer.InvariantCultureIgnoreCase));
                cacheURL=url;
            }
            return handler!=null;
        }

        protected async override Task ExecuteActionHandlerAsync(HttpContext context, string url, IModelActionHandler handler)
        {
            ModelRequestData requestData = await ExtractParts(context);
            var model = (IModel)Activator.CreateInstance(handler.GetType().GetGenericArguments()[0]!)!;
            model = Utility.SetModelValues(requestData, model, true, Log);
            await handler.InvokeWithoutLoad(url, requestData, context, model, extractResponse: (model, response, pars, method) =>
            {
                if ((bool?)response??false)
                    return new Hashtable() { { "id", model.id } };
                throw new SaveFailedException(model.GetType(), method);
            });
        }

        protected override IEnumerable<IModelActionHandler> GetHandlers(IEnumerable<Type> types)
            => types
                .Select(t => new
                {
                    type = t,
                    saveMethod = Array.Find(t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS), m => m.GetCustomAttribute<ModelSaveMethodAttribute>(false)!=null)
                })
                .Where(pair => pair.saveMethod!=null)
                .Select(pair =>
                    (IModelActionHandler)Activator.CreateInstance(
                        typeof(ModelActionHandler<>).MakeGenericType(pair.type),
                        pair.saveMethod,
                        "save",
                        registerSlowMethod,
                        Log
                    )!
                );

        protected override void RemoveHandlers(IEnumerable<Type> types, ref List<IModelActionHandler> handlers)
            => handlers.RemoveAll(h =>
                types.Contains(h.GetType().GetGenericArguments()[0])
            );
    }
}
