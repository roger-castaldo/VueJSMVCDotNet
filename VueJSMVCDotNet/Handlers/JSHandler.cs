using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.JSGenerators;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers
{
    internal class JSHandler : IRequestHandler
    {
        private static readonly IJSGenerator[] _generators = new IJSGenerator[]
        {
            new HeaderGenerator(),
            new JSONGenerator(),
            new ParsersGenerator(),
            new ModelDefinitionGenerator(),
            new StaticCallsHeaderGenerator(),
            new ModelLoadAllGenerator(),
            new ModelLoadGenerator(),
            new StaticMethodGenerator(),
            new ModelListCallGenerator(),
            new ExtendMethodGenerator(),
            new FooterGenerator()
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
            lock (_cache) {
                if (_cache.ContainsKey(url))
                    ret = true;
            }
            if(!ret && _types!=null)
            {
                foreach (Type t in _types)
                {
                    if (RequestHandler.IsTypeAllowed(t))
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
            }
            return ret;
        }

        public string HandleRequest(string url, RequestHandler.RequestMethods method, string formData, out string contentType, out int responseStatus)
        {
            if (!HandlesRequest(url, method))
            {
                contentType = "text/text";
                responseStatus = 404;
                return "Not Found";
            }
            else
            {
                string ret = null;
                contentType = "text/javascript";
                responseStatus = 200;
                lock (_cache)
                {
                    if (_cache.ContainsKey(url))
                        ret = _cache[url];
                }
                if (ret == null && _types!=null)
                {
                    Type model = null;
                    foreach (Type t in _types)
                    {
                        foreach (ModelJSFilePath mjsfp in t.GetCustomAttributes(typeof(ModelJSFilePath), false))
                        {
                            if (mjsfp.IsMatch(url))
                            {
                                model = t;
                                break;
                            }
                        }
                    }
                    WrappedStringBuilder builder = new WrappedStringBuilder(url.ToLower().EndsWith(".min.js"));
                    foreach (IJSGenerator gen in _generators)
                        gen.GeneratorJS(ref builder, url.ToLower().EndsWith(".min.js"), model);
                    ret = builder.ToString();
                    lock (_cache)
                    {
                        if (!_cache.ContainsKey(url))
                            _cache.Add(url, ret);
                    }
                }
                return ret;
            }
        }

        public void Init(List<Type> types)
        {
            _types = types;
        }
    }
}
