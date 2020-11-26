using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.JSGenerators;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers
{
    internal class JSHandler : IRequestHandler
    {
        private static readonly IBasicJSGenerator[] _oneTimeInitialGenerators = new IBasicJSGenerator[]{
            new HeaderGenerator()
        };

        private static readonly IBasicJSGenerator[] _oneTimeFinishGenerators = new IBasicJSGenerator[]{
            new FooterGenerator()
        };

        private static readonly IJSGenerator[] _instanceGenerators = new IJSGenerator[]
        {
            new ModelInstanceHeaderGenerator(),
            new JSONGenerator(),
            new ParsersGenerator(),
            new ModelDefinitionGenerator(),
            new ModelInstanceFooterGenerator()
        };

        private static readonly IJSGenerator[] _globalGenerators = new IJSGenerator[]
        {
            new ModelLoadAllGenerator(),
            new ModelLoadGenerator(),
            new StaticMethodGenerator(),
            new ModelListCallGenerator()
        };

        private Dictionary<string, string> _cache;
        private List<Type> _types;

        public JSHandler()
        {
            _cache = new Dictionary<string, string>();
        }

        public void ClearCache()
        {
            lock (_cache)
            {
                _cache.Clear();
            }
        }

        public bool HandlesRequest(string url, RequestHandler.RequestMethods method)
        {
            if (method != RequestHandler.RequestMethods.GET)
                return false;
            bool ret = false;
            lock (_cache)
            {
                if (_cache.ContainsKey(url))
                    ret = true;
            }
            if (!ret && _types != null)
            {
                foreach (Type t in _types)
                {
                    foreach (ModelJSFilePath mjsfp in t.GetCustomAttributes(typeof(ModelJSFilePath), false))
                    {
                        if (mjsfp.IsMatch(url))
                        {
                            ret = true;
                            break;
                        }
                    }
                }
            }
            return ret;
        }

        public Task HandleRequest(string url, RequestHandler.RequestMethods method, string formData, HttpContext context, ISecureSession session, IsValidCall securityCheck)
        {
            if (!HandlesRequest(url, method))
                throw new CallNotFoundException();
            else
            {
                List<Type> models = new List<Type>();
                if (_types != null)
                {
                    foreach (Type t in _types)
                    {
                        foreach (ModelJSFilePath mjsfp in t.GetCustomAttributes(typeof(ModelJSFilePath), false))
                        {
                            if (mjsfp.IsMatch(url))
                                models.Add(t);
                        }
                    }
                    foreach (Type model in models){
                        if (!securityCheck.Invoke(model, null, session,null,url,null))
                            throw new InsecureAccessException();
                    }
                }
                string ret = null;
                context.Response.ContentType= "text/javascript";
                context.Response.StatusCode= 200;
                lock (_cache)
                {
                    if (_cache.ContainsKey(url))
                        ret = _cache[url];
                }
                if (ret == null && models.Count>0)
                {
                    WrappedStringBuilder builder = new WrappedStringBuilder(url.ToLower().EndsWith(".min.js"));
                    foreach (IBasicJSGenerator gen in _oneTimeInitialGenerators)
                        gen.GeneratorJS(ref builder);
                    foreach (Type model in models){
                        foreach (IJSGenerator gen in _instanceGenerators)
                            gen.GeneratorJS(ref builder, model);
                        foreach (IJSGenerator gen in _globalGenerators)
                            gen.GeneratorJS(ref builder, model);
                    }
                    foreach (IBasicJSGenerator gen in _oneTimeFinishGenerators)
                        gen.GeneratorJS(ref builder);
                    ret = builder.ToString();
                    lock (_cache)
                    {
                        if (!_cache.ContainsKey(url))
                            _cache.Add(url, ret);
                    }
                }
                return context.Response.WriteAsync(ret);
            }
        }

        public void Init(List<Type> types)
        {
            _types = types;
        }
    }
}
