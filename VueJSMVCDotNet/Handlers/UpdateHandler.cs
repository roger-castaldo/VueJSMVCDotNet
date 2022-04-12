﻿using Microsoft.AspNetCore.Http;
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

        public Task HandleRequest(string url, RequestHandler.RequestMethods method, string formData, HttpContext context, ISecureSession session, IsValidCall securityCheck)
        {
            IModel model = null;
            lock (_loadMethods)
            {
                if (_loadMethods.ContainsKey(url.Substring(0, url.LastIndexOf("/"))))
                {
                    if (!securityCheck.Invoke(_loadMethods[url.Substring(0, url.LastIndexOf("/"))].DeclaringType, _loadMethods[url.Substring(0, url.LastIndexOf("/"))], session,null,url,new Hashtable() { {"id", url.Substring(0, url.LastIndexOf("/")) } }))
                        throw new InsecureAccessException();
                    model = (IModel)_loadMethods[url.Substring(0, url.LastIndexOf("/"))].Invoke(null, new object[] { url.Substring(url.LastIndexOf("/") + 1) });
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
                    if (!securityCheck.Invoke(mi.DeclaringType, mi, session,model,url,(Hashtable)JSON.JsonDecode(formData)))
                        throw new InsecureAccessException();
                    context.Response.ContentType = "text/json";
                    context.Response.StatusCode= 200;
                    Utility.SetModelValues(formData, ref model, false);
                    return context.Response.WriteAsync(JSON.JsonEncode(mi.Invoke(model, new object[] { })));
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
        }
    }
}
