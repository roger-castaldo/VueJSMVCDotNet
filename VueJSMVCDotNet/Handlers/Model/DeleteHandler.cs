using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
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
        private Dictionary<string, MethodInfo> _loadMethods;
        private Dictionary<string, MethodInfo> _deleteMethods;

        public DeleteHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase)
            :base(next,sessionFactory,registerSlowMethod,urlBase)
        {
            _loadMethods = new Dictionary<string, MethodInfo>();
            _deleteMethods = new Dictionary<string, MethodInfo>();
        }

        public override void ClearCache()
        {
            _loadMethods.Clear();
            _deleteMethods.Clear();
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            var url = _CleanURL(context);
            if (GetRequestMethod(context)==ModelRequestHandler.RequestMethods.DELETE && _deleteMethods.ContainsKey(url.Substring(0, url.LastIndexOf("/")))&& _loadMethods.ContainsKey(url.Substring(0, url.LastIndexOf("/"))))
            {
                Logger.Debug("Attempting to execute the Delete Handler for the path {0}", new object[] { url });
                IModel model = null;
                MethodInfo loadMethod = null;
                sRequestData requestData = await _ExtractParts(context);
                lock (_loadMethods)
                {
                    Logger.Trace("Trying to find a load method matching the url {0}", new object[] { url });
                    if (_loadMethods.ContainsKey(url.Substring(0, url.LastIndexOf("/"))))
                        loadMethod = _loadMethods[url.Substring(0, url.LastIndexOf("/"))];
                }
                if (loadMethod!=null)
                {
                    if (!await _ValidCall(loadMethod.DeclaringType, loadMethod, model, context))
                        throw new InsecureAccessException();
                    Logger.Trace("Attempting to load model at url {0}", new object[] { url });
                    model = Utility.InvokeLoad(_loadMethods[url.Substring(0, url.LastIndexOf("/"))], url.Substring(url.LastIndexOf("/") + 1), requestData.Session);
                }
                if (model == null)
                    throw new CallNotFoundException("Model Not Found");
                else
                {
                    Logger.Trace("Trying to find a delete method matching the url {0}", new object[] { url });
                    MethodInfo deleteMethod = null;
                    lock (_deleteMethods)
                    {
                        if (_deleteMethods.ContainsKey(url.Substring(0, url.LastIndexOf("/"))))
                            deleteMethod = _deleteMethods[url.Substring(0, url.LastIndexOf("/"))];
                    }
                    if (deleteMethod != null)
                    {
                        if (!await _ValidCall(loadMethod.DeclaringType, deleteMethod, model, context))
                            throw new InsecureAccessException();
                        else
                        {
                            Logger.Trace("Invoking the delete method {0}.{1} for the url {2}", new object[] { deleteMethod.DeclaringType.FullName, deleteMethod.Name, url });
                            context.Response.ContentType = "text/json";
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync(JSON.JsonEncode(Utility.InvokeMethod(deleteMethod, model, session: requestData.Session)));
                        }
                    }
                    else
                        throw new CallNotFoundException("Method Not Found");
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
                    lock (_deleteMethods)
                    {
                        _deleteMethods.Add(Utility.GetModelUrlRoot(t), delMethod);
                    }
                    MethodInfo loadMethod = t.GetMethods(Constants.LOAD_METHOD_FLAGS).Where(m => m.GetCustomAttributes(typeof(ModelLoadMethod), false).Length>0).FirstOrDefault();
                    lock (_loadMethods)
                    {
                        _loadMethods.Add(Utility.GetModelUrlRoot(t), loadMethod);
                    }
                }
            }
        }

        protected override void _UnloadTypes(List<Type> types)
        {
            string[] keys;
            lock (_deleteMethods)
            {
                keys = new string[_deleteMethods.Count];
                _deleteMethods.Keys.CopyTo(keys, 0);
                foreach (string str in keys)
                {
                    if (types.Contains(_deleteMethods[str].DeclaringType))
                    {
                        _deleteMethods.Remove(str);
                    }
                }
            }
            lock (_loadMethods)
            {
                keys = new string[_loadMethods.Count];
                _loadMethods.Keys.CopyTo(keys, 0);
                foreach (string str in keys)
                {
                    if (types.Contains(_loadMethods[str].DeclaringType))
                    {
                        _loadMethods.Remove(str);
                    }
                }
            }
        }
    }
}
