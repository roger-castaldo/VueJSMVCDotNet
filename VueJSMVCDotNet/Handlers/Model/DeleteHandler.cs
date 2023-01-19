using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.ModelRequestHandler;
using static System.Collections.Specialized.BitVector32;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model
{
    internal class DeleteHandler : ModelRequestHandlerBase
    {
        struct sMethodPair
        {
            private readonly MethodInfo _loadMethod;
            public MethodInfo LoadMethod => _loadMethod;
            private readonly MethodInfo _deleteMethod;
            public MethodInfo DeleteMethod => _deleteMethod;

            public Type DeclaringType => _loadMethod.DeclaringType;

            public sMethodPair(MethodInfo loadMethod,MethodInfo deleteMethod)
            {
                _loadMethod= loadMethod;
                _deleteMethod= deleteMethod;
            }
        }

        private ConcurrentDictionary<string, sMethodPair> _methods;

        public DeleteHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase)
            :base(next,sessionFactory,registerSlowMethod,urlBase)
        {
            _methods=new ConcurrentDictionary<string, sMethodPair>();
        }

        public override void ClearCache()
        {
            _methods.Clear();
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            var url = _CleanURL(context);
            if (GetRequestMethod(context)==ModelRequestHandler.RequestMethods.DELETE && _methods.ContainsKey(url.Substring(0, url.LastIndexOf("/"))))
            {
                Logger.Debug("Attempting to execute the Delete Handler for the path {0}", new object[] { url });
                IModel model = null;
                MethodInfo loadMethod = null;
                MethodInfo deleteMethod = null;
                Logger.Trace("Trying to find a load method matching the url {0}", new object[] { url });
                sMethodPair pair;
                if (_methods.TryGetValue(url.Substring(0, url.LastIndexOf("/")),out pair))
                {
                    loadMethod=pair.LoadMethod;
                    deleteMethod= pair.DeleteMethod;    
                }
                sRequestData requestData = await _ExtractParts(context);
                if (loadMethod!=null)
                {
                    if (!await _ValidCall(loadMethod.DeclaringType, loadMethod, model, context))
                        throw new InsecureAccessException();
                    Logger.Trace("Attempting to load model at url {0}", new object[] { url });
                    model = Utility.InvokeLoad(loadMethod, url.Substring(url.LastIndexOf("/") + 1), requestData.Session);
                }
                if (model == null)
                    throw new CallNotFoundException("Model Not Found");
                else
                {
                    Logger.Trace("calling delete method matching the url {0}", new object[] { url });
                    if (!await _ValidCall(deleteMethod.DeclaringType, deleteMethod, model, context))
                        throw new InsecureAccessException();
                    else
                    {
                        Logger.Trace("Invoking the delete method {0}.{1} for the url {2}", new object[] { deleteMethod.DeclaringType.FullName, deleteMethod.Name, url });
                        context.Response.ContentType = "text/json";
                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync(JSON.JsonEncode(Utility.InvokeMethod(deleteMethod, model, session: requestData.Session)));
                    }
                }
            }
            else
                await _next(context);
        }

        protected override void _LoadTypes(List<Type> types)
        {
            foreach (Type t in types)
            {
                MethodInfo delMethod = t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).Where(m => m.GetCustomAttributes(typeof(ModelDeleteMethod), false).Length>0).FirstOrDefault();
                if (delMethod != null)
                {
                    _methods.TryAdd(Utility.GetModelUrlRoot(t), new sMethodPair(
                        t.GetMethods(Constants.LOAD_METHOD_FLAGS).Where(m => m.GetCustomAttributes(typeof(ModelLoadMethod), false).Length>0).FirstOrDefault(), 
                        delMethod
                    ));
                }
            }
        }

        protected override void _UnloadTypes(List<Type> types)
        {
            var keys = new string[_methods.Count];
            _methods.Keys.CopyTo(keys, 0);
            sMethodPair pair;
            foreach (string str in keys)
            {
                if (types.Contains(_methods[str].DeclaringType))
                    _methods.TryRemove(str, out pair);
            }
        }
    }
}
