using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.ModelRequestHandler;
using static System.Collections.Specialized.BitVector32;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model
{
    internal class SaveHandler : ModelRequestHandlerBase
    {
        private Dictionary<string, ConstructorInfo> _constructors;
        private Dictionary<string, MethodInfo> _saveMethods;

        public SaveHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase)
            :base(next,sessionFactory,registerSlowMethod,urlBase)
        {
            _constructors = new Dictionary<string, ConstructorInfo>();
            _saveMethods = new Dictionary<string, MethodInfo>();
        }

        public override void ClearCache()
        {
            _constructors.Clear();
            _saveMethods.Clear();
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            string url = _CleanURL(context);
            Logger.Trace("Checking to see if {0}:{1} is handled by the Save Handler", new object[] { GetRequestMethod(context), url });
            if (GetRequestMethod(context)==ModelRequestHandler.RequestMethods.PUT)
            {
                MethodInfo mi = null;
                lock (_saveMethods)
                {
                    mi = (_saveMethods.ContainsKey(url) ? _saveMethods[url] : null);
                }
                if (mi!=null)
                {
                    IModel model = null;
                    lock (_constructors)
                    {
                        model = (IModel)(_constructors.ContainsKey(url) ? _constructors[url].Invoke(new object[] { }) : null);
                    }
                    if (model == null)
                        throw new CallNotFoundException("Model Not Found");
                    else
                    {
                        if (!await _ValidCall(mi.DeclaringType,mi,model,context))
                            throw new InsecureAccessException();
                        Logger.Trace("Attempting to handle a save request with {0}.{1} in the model with id {2}", new object[] { model.GetType().FullName, mi.Name, model.id });
                        sRequestData requestData = await _ExtractParts(context);
                        Utility.SetModelValues(requestData.FormData, ref model, true);
                        if ((bool)Utility.InvokeMethod(mi, model, session: requestData.Session))
                        {
                            context.Response.ContentType = "text/json";
                            context.Response.StatusCode= 200;
                            await context.Response.WriteAsync(JSON.JsonEncode(new Hashtable() { { "id", model.id } }));
                            return;
                        }
                        throw new Exception("Failed");
                    }
                }
            }
            await _next(context);
        }

       protected override void _LoadTypes(List<Type> types){
            foreach (Type t in types)
            {
                MethodInfo saveMethod = t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).Where(m => m.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0).FirstOrDefault();
                if (saveMethod != null)
                {
                    _saveMethods.Add(Utility.GetModelUrlRoot(t), saveMethod);
                    _constructors.Add(Utility.GetModelUrlRoot(t), t.GetConstructor(Type.EmptyTypes));
                }
            }
        }

        protected override void _UnloadTypes(List<Type> types)
        {
            string[] keys;
            lock (_saveMethods)
            {
                keys = new string[_saveMethods.Count];
                _saveMethods.Keys.CopyTo(keys, 0);
                foreach (string str in keys)
                {
                    if (types.Contains(_saveMethods[str].DeclaringType))
                    {
                        _saveMethods.Remove(str);
                    }
                }
            }
            lock (_constructors)
            {
                keys = new string[_constructors.Count];
                _constructors.Keys.CopyTo(keys, 0);
                foreach (string str in keys)
                {
                    if (types.Contains(_constructors[str].DeclaringType))
                    {
                        _constructors.Remove(str);
                    }
                }
            }
        }
    }
}
