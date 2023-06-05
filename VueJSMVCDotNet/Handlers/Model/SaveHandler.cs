using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;
using static System.Collections.Specialized.BitVector32;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class SaveHandler : ModelRequestHandlerBase
    {
        private readonly List<IModelActionHandler> _handlers;

        public SaveHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase,ILog log)
            :base(next,sessionFactory,registerSlowMethod,urlBase,log)
        {
            _handlers=new List<IModelActionHandler>();
        }

        public override void ClearCache()
        {
            _handlers.Clear();
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            string url = _CleanURL(context);
            log.Trace("Checking to see if {0}:{1} is handled by the Save Handler", new object[] { GetRequestMethod(context), url });
            if (GetRequestMethod(context)==ModelRequestHandler.RequestMethods.PUT && _handlers.Any(h => h.BaseURLs.Contains(url, StringComparer.InvariantCultureIgnoreCase)))
            {
                var handler = _handlers.FirstOrDefault(h => h.BaseURLs.Contains(url, StringComparer.InvariantCultureIgnoreCase));
                if (handler==null)
                    throw new CallNotFoundException("Model Not Found");
                ModelRequestData requestData = await _ExtractParts(context);
                var model = (IModel)Activator.CreateInstance(handler.GetType().GetGenericArguments()[0]);
                Utility.SetModelValues(requestData, ref model, true,log);
                await handler.InvokeWithoutLoad(url, requestData, context, model, extractResponse: (model, response,pars,method) =>
                {
                    if ((bool)response)
                        return new Hashtable() { {"id", model.id }};
                    throw new SaveFailedException(model.GetType(), method);
                });
            }
            await _next(context);
        }

       protected override void _LoadTypes(List<Type> types){
            foreach (Type t in types)
            {
                MethodInfo saveMethod = t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).FirstOrDefault(m => m.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0);
                if (saveMethod != null)
                {
                    _handlers.Add((IModelActionHandler)
                        typeof(ModelActionHandler<>).MakeGenericType(new Type[] { t })
                        .GetConstructor(new Type[] { typeof(MethodInfo), typeof(string), typeof(delRegisterSlowMethodInstance), typeof(ILog) })
                        .Invoke(new object[] { saveMethod, "save", _registerSlowMethod, log })
                    );
                }
            }
        }

        protected override void _UnloadTypes(List<Type> types)
        {
            _handlers.RemoveAll(h =>
                types.Contains(h.GetType().GetGenericArguments()[0])
            );
        }
    }
}
