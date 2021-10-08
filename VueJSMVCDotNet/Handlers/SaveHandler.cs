using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers
{
    internal class SaveHandler : IRequestHandler
    {
        private Dictionary<string, ConstructorInfo> _constructors;
        private Dictionary<string, MethodInfo> _saveMethods;

        public SaveHandler()
        {
            _constructors = new Dictionary<string, ConstructorInfo>();
            _saveMethods = new Dictionary<string, MethodInfo>();
        }

        public void ClearCache()
        {
            _constructors.Clear();
            _saveMethods.Clear();
        }

        public Task HandleRequest(string url, RequestHandler.RequestMethods method, Hashtable formData, HttpContext context, ISecureSession session, IsValidCall securityCheck)
        {
            Logger.Trace("Attempting to handle the request {0}:{1} with the Save Handler", new object[] { method, url });
            IModel model = null;
            lock (_constructors)
            {
                if (_constructors.ContainsKey(url))
                    model = (IModel)_constructors[url].Invoke(new object[] { });
            }
            if (model == null)
                throw new CallNotFoundException("Model Not Found");
            else
            {
                MethodInfo mi = null;
                lock (_saveMethods)
                {
                    if (_saveMethods.ContainsKey(url))
                        mi = _saveMethods[url];
                }
                if (mi != null)
                {
                    if (!securityCheck.Invoke(mi.DeclaringType, mi, session,null,url,formData))
                        throw new InsecureAccessException();
                    Logger.Trace("Attempting to handle a save request with {0}.{1} in the model with id {2}", new object[] { model.GetType().FullName, mi.Name, model.id });
                    Utility.SetModelValues(formData, ref model, true);
                    if ((bool)mi.Invoke(model, (mi.GetParameters().Length==1 ? new object[]{session} : new object[] { })))
                    {
                        context.Response.ContentType = "text/json";
                        context.Response.StatusCode= 200;
                        return context.Response.WriteAsync(JSON.JsonEncode(new Hashtable() { { "id", model.id } }));
                    }
                    throw new Exception("Failed");
                }
                throw new CallNotFoundException();
            }
        }

        public bool HandlesRequest(string url, RequestHandler.RequestMethods method)
        {
            Logger.Trace("Checking to see if {0}:{1} is handled by the Save Handler", new object[] { method, url });
            if (method == RequestHandler.RequestMethods.PUT)
                return _saveMethods.ContainsKey(url)&& _constructors.ContainsKey(url);
            return false;
        }

        public void Init(List<Type> types)
        {
            lock (_saveMethods)
            {
                _saveMethods.Clear();
                _constructors.Clear();
                _LoadTypes(types);
            }
        }

        private void _LoadTypes(List<Type> types){
            foreach (Type t in types)
            {
                foreach (MethodInfo mi in t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS))
                {
                    if (mi.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0)
                    {
                        _saveMethods.Add(Utility.GetModelUrlRoot(t), mi);
                        _constructors.Add(Utility.GetModelUrlRoot(t), t.GetConstructor(Type.EmptyTypes));
                        break;
                    }
                }
            }
        }

        #if NETCOREAPP3_1
        public void LoadTypes(List<Type> types){
            lock(_saveMethods){
                _LoadTypes(types);
            }
        }
        public void UnloadTypes(List<Type> types){
            string[] keys;
            lock(_saveMethods){
                keys = new string[_saveMethods.Count];
                _saveMethods.Keys.CopyTo(keys,0);
                foreach (string str in keys){
                    if (types.Contains(_saveMethods[str].DeclaringType)){
                        _saveMethods.Remove(str);
                    }
                }
            }
            lock(_constructors){
                keys = new string[_constructors.Count];
                _constructors.Keys.CopyTo(keys,0);
                foreach (string str in keys){
                    if (types.Contains(_constructors[str].DeclaringType)){
                        _constructors.Remove(str);
                    }
                }
            }
        }
        #endif
    }
}
