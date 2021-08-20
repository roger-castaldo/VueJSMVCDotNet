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
    internal class UpdateHandler : IRequestHandler
    {
        private Dictionary<string, MethodInfo> _loadMethods;
        private Dictionary<string, MethodInfo> _updateMethods;

        public UpdateHandler()
        {
            _loadMethods = new Dictionary<string, MethodInfo>();
            _updateMethods = new Dictionary<string, MethodInfo>();
        }

        public void ClearCache()
        {
            _loadMethods.Clear();
            _updateMethods.Clear();
        }

        public Task HandleRequest(string url, RequestHandler.RequestMethods method, Hashtable formData, HttpContext context, ISecureSession session, IsValidCall securityCheck)
        {
            IModel model = null;
            lock (_loadMethods)
            {
                if (_loadMethods.ContainsKey(url.Substring(0, url.LastIndexOf("/"))))
                {
                    if (!securityCheck.Invoke(_loadMethods[url.Substring(0, url.LastIndexOf("/"))].DeclaringType, _loadMethods[url.Substring(0, url.LastIndexOf("/"))], session,null,url,new Hashtable() { {"id", url.Substring(0, url.LastIndexOf("/")) } }))
                        throw new InsecureAccessException();
                    model = Utility.InvokeLoad(_loadMethods[url.Substring(0, url.LastIndexOf("/"))],url.Substring(url.LastIndexOf("/") + 1),session);
                }
            }
            if (model == null)
                throw new CallNotFoundException("Model Not Found");
            else
            {
                MethodInfo mi = null;
                lock (_updateMethods)
                {
                    if (_updateMethods.ContainsKey(url.Substring(0, url.LastIndexOf("/"))))
                        mi = _updateMethods[url.Substring(0, url.LastIndexOf("/"))];
                }
                if (mi != null)
                {
                    if (!securityCheck.Invoke(mi.DeclaringType, mi, session,model,url,formData))
                        throw new InsecureAccessException();
                    context.Response.ContentType = "text/json";
                    context.Response.StatusCode= 200;
                    Utility.SetModelValues(formData, ref model, false);
                    return context.Response.WriteAsync(JSON.JsonEncode(mi.Invoke(model, (mi.GetParameters().Length == 1 ? new object[]{session} : new object[] { }))));
                }
                throw new CallNotFoundException();
            }
        }

        public bool HandlesRequest(string url, RequestHandler.RequestMethods method)
        {
            if (method == RequestHandler.RequestMethods.PATCH)
                return _updateMethods.ContainsKey(url.Substring(0, url.LastIndexOf("/")))&& _loadMethods.ContainsKey(url.Substring(0, url.LastIndexOf("/")));
            return false;
        }

        public void Init(List<Type> types)
        {
            lock (_updateMethods)
            {
                _loadMethods.Clear();
                _updateMethods.Clear();
                _LoadTypes(types);
            }
        }

        private void _LoadTypes(List<Type> types){
            foreach (Type t in types)
            {
                foreach (MethodInfo mi in t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS))
                {
                    if (mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0)
                    {
                        _updateMethods.Add(Utility.GetModelUrlRoot(t), mi);
                        foreach (MethodInfo m in t.GetMethods(Constants.LOAD_METHOD_FLAGS))
                        {
                            if (m.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0)
                            {
                                _loadMethods.Add(Utility.GetModelUrlRoot(t), m);
                                break;
                            }
                        }
                        break;
                    }
                }
            }
        }

        #if NETCOREAPP3_1
        public void LoadTypes(List<Type> types){
            lock(_updateMethods){
                _LoadTypes(types);
            }
        }
        public void UnloadTypes(List<Type> types){
            string[] keys;
            lock(_updateMethods){
                keys = new string[_updateMethods.Count];
                _updateMethods.Keys.CopyTo(keys,0);
                foreach (string str in keys){
                    if (types.Contains(_updateMethods[str].DeclaringType)){
                        _updateMethods.Remove(str);
                    }
                }
            }
            lock(_loadMethods){
                keys = new string[_loadMethods.Count];
                _loadMethods.Keys.CopyTo(keys,0);
                foreach (string str in keys){
                    if (types.Contains(_loadMethods[str].DeclaringType)){
                        _loadMethods.Remove(str);
                    }
                }
            }
        }
        #endif
    }
}
