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
    internal class LoadHandler : INonCachingRequestHandler
    {
        private Dictionary<string, MethodInfo> _methods;

        public LoadHandler()
        {
            _methods = new Dictionary<string, MethodInfo>();
        }

        public void ClearCache()
        {
            _methods.Clear();
        }

        public Task HandleRequest(string url, ModelRequestHandler.RequestMethods method, System.Collections.Hashtable formData, HttpContext context, ISecureSession session, IsValidCall securityCheck)
        {
            Logger.Trace("Attempting to handle {0}:{1} inside the Load Handler", new object[] { method, url });
            MethodInfo mi = null;
            lock (_methods)
            {
                if (_methods.ContainsKey(url.Substring(0, url.LastIndexOf("/"))))
                    mi = _methods[url.Substring(0, url.LastIndexOf("/"))];
            }
            if (mi != null)
            {
                if (!securityCheck.Invoke(mi.DeclaringType, mi, session,null,url,new System.Collections.Hashtable() { { "id", url.Substring(url.LastIndexOf("/") + 1) } }))
                    throw new InsecureAccessException();
                context.Response.ContentType = "text/json";
                context.Response.StatusCode= 200;
                string id = url.Substring(url.LastIndexOf("/")+1);
                Logger.Trace("Attempting to load model using {0}.{1} with the id {2}", new object[] { mi.DeclaringType.FullName, mi.Name, id });
                return context.Response.WriteAsync(JSON.JsonEncode(Utility.InvokeLoad(mi,id,session)));
            }
            throw new CallNotFoundException();
        }

        public bool HandlesRequest(string url, ModelRequestHandler.RequestMethods method)
        {
            Logger.Trace("Checking if the Load Handler handles {0}:{1}", new object[] { method, url });
            if (method == ModelRequestHandler.RequestMethods.GET)
                return _methods.ContainsKey(url.Substring(0,url.LastIndexOf("/")));
            return false;
        }

        public void Init(List<Type> types)
        {
            lock (_methods)
            {
                _methods.Clear();
                _LoadTypes(types);
            }
        }

        private void _LoadTypes(List<Type> types){
            foreach (Type t in types)
            {
                foreach (MethodInfo mi in t.GetMethods(Constants.LOAD_METHOD_FLAGS))
                {
                    if (mi.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0)
                    {
                        _methods.Add(Utility.GetModelUrlRoot(t), mi);
                        break;
                    }
                }
            }
        }

        #if NET
        public void LoadTypes(List<Type> types){
            lock(_methods){
                _LoadTypes(types);
            }
        }
        public void UnloadTypes(List<Type> types){
            lock(_methods){
                string[] keys = new string[_methods.Count];
                _methods.Keys.CopyTo(keys,0);
                foreach (string str in keys){
                    if (types.Contains(_methods[str].DeclaringType)){
                        _methods.Remove(str);
                    }
                }
            }
        }
        #endif
    }
}
