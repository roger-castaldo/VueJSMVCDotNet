using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers
{
    internal class ModelListCallHandler : ICachingRequestHandler
    {
        private struct sModelListCall : IComparable<sModelListCall>
        {
            private static readonly Regex _regParameter = new Regex("\\{(\\d+)\\}", RegexOptions.Compiled | RegexOptions.ECMAScript);

            private Regex _reg;
            private string _url;
            private MethodInfo _method;
            private Dictionary<int,int> _groupIndexes;
            private bool _isPaged;
            private int _sessionIndex;
            private bool _usesSession;
            private int _logIndex;
            private bool _usesLog;

            public sModelListCall(ModelListMethod mlm,MethodInfo mi)
            {
                _isPaged = mlm.Paged;
                string reg = "";
                _groupIndexes = null;
                _usesSession = Utility.UsesSecureSession(mi,out _sessionIndex);
                _usesLog=Utility.UsesLog(mi, out _logIndex);
                ParameterInfo[] pars = Utility.ExtractStrippedParameters(mi);
                if (pars.Length > 0)
                {
                    string[] regexs = new string[pars.Length];
                    _url = (mlm.Path + (mlm.Paged ? (mlm.Path.Contains("?") ? "&" : "?") + "PageStartIndex={" + (regexs.Length - 3).ToString() + "}&PageSize={" + (regexs.Length - 2).ToString() + "}" : ""));
                    reg = _url.Replace("?", "\\?");
                    for (int x = 0; x < pars.Length; x++)
                    {
                        Type ptype = pars[x].ParameterType;
                        bool nullable = false;
                        regexs[x] = "(.+)";
                        if (ptype.FullName.StartsWith("System.Nullable"))
                        {
                            nullable = true;
                            if (ptype.IsGenericType)
                                ptype = ptype.GetGenericArguments()[0];
                            else
                                ptype = ptype.GetElementType();
                        }
                        if (ptype == typeof(DateTime))
                            regexs[x] = "(\\d+" + (nullable ? "|NULL" : "") + ")";
                        else if (ptype == typeof(int) ||
                            ptype == typeof(long) ||
                            ptype == typeof(short) ||
                            ptype == typeof(byte))
                            regexs[x] = "(-?\\d+" + (nullable ? "|NULL" : "") + ")";
                        else if (ptype == typeof(uint) ||
                            ptype == typeof(ulong) ||
                            ptype == typeof(ushort))
                            regexs[x] = "(\\d+" + (nullable ? "|NULL" : "") + ")";
                        else if (ptype == typeof(double) ||
                            ptype == typeof(decimal) ||
                            ptype == typeof(float))
                            regexs[x] = "(-?\\d+(.\\d+)?([Ee][+-]\\d+)?" + (nullable ? "|NULL" : "") + ")";
                        else if (ptype == typeof(bool))
                            regexs[x] = "(true|false" + (nullable ? "|NULL" : "") + ")";
                        else if (ptype.IsEnum)
                        {
                            regexs[x] = "(";
                            foreach (string str in Enum.GetNames(ptype))
                                regexs[x] += str + "|";
                            regexs[x] = regexs[x].Substring(0, regexs[x].Length - 1) + (nullable ? "|NULL" : "") + ")";
                        }
                        else if (ptype == typeof(Guid))
                        {
                            regexs[x] = "([0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}"+(nullable ? "|NULL" : "")+")";
                        }
                    }
                    _groupIndexes = new Dictionary<int, int>();
                    MatchCollection matches = _regParameter.Matches(reg);
                    for (int x = 0; x < matches.Count; x++)
                    {
                        int idx = int.Parse(matches[x].Groups[1].Value);
                        if (_usesSession)
                        {
                            if (idx>=_sessionIndex)
                                idx++;
                        }
                        if (_usesLog)
                        {
                            if (idx>=_logIndex)
                                idx++;
                        }
                        _groupIndexes.Add(idx, x);
                    }
                    reg = string.Format(reg, regexs);
                    reg = (reg.StartsWith("/") ? reg : "/" + reg).TrimEnd('/');
                }
                else
                {
                    _url = mlm.Path;
                    reg = _url.Replace("?", "\\?");
                }
                _reg = new Regex(string.Format("^{0}$",reg), RegexOptions.Compiled|RegexOptions.ECMAScript);
                _method = mi;
            }

            #if NET
            public bool IsForType(Type type){
                return _method.DeclaringType == type;
            }
            #endif

            public bool IsValid(string url)
            {
                return _reg.IsMatch(url);
            }

            public bool ConvertParameters(string url,out object[] opars)
            {
                Logger.Trace("Converting url parameters from {0} to be handled by the model list call {1}.{2}", new object[] { url, _method.GetType().FullName, _method.Name });
                ParameterInfo[] pars = Utility.ExtractStrippedParameters(_method);
                opars = null;
                if (pars.Length > 0)
                {
                    opars = new object[pars.Length];
                    Match m = _reg.Match(url);
                    foreach (int x in _groupIndexes.Keys)
                    {
                        try
                        {
                            opars[x] = _ConvertParameterValue(m.Groups[_groupIndexes[x] + 1].Value, pars[x].ParameterType);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex);
                            return false;
                        }
                    }
                }
                return true;
            }

            public Task HandleRequest(string url, ModelRequestHandler.RequestMethods method, Hashtable formData, HttpContext context, ISecureSession session, IsValidCall securityCheck, object[] opars)
            {
                if (!securityCheck.Invoke(_method.DeclaringType, _method, session,null,url,formData))
                    throw new InsecureAccessException();
                context.Response.ContentType = "text/json";
                context.Response.StatusCode= 200;
                ParameterInfo[] pars = Utility.ExtractStrippedParameters(_method);
                Logger.Trace("Invoking method {0}.{1} for {2}", new object[] { _method.GetType().FullName, _method.Name, url });
                object ret = Utility.InvokeMethod(_method, null, pars: opars, session: session);
                if (_isPaged)
                {
                    int pageIndex = opars.Length-1;
                    for(int x = 0; x<pars.Length; x++)
                    {
                        if (pars[x].IsOut)
                        {
                            pageIndex=x;
                            break;
                        }
                    }
                    Logger.Trace("Outputting page information TotalPages:{0} for {1}:{2}", new object[] { opars[pageIndex], method, url });
                    return context.Response.WriteAsync(JSON.JsonEncode(new Hashtable()
                        {
                            {"response",ret },
                            {"TotalPages",opars[pageIndex] }
                        }));
                }
                else
                    return context.Response.WriteAsync(JSON.JsonEncode(ret));
            }

            private object _ConvertParameterValue(string p, Type type)
            {
                Logger.Trace("Attempting to convert url parameter {0} to the type {1}", new object[] { p, type.FullName });
                p = Uri.UnescapeDataString(p);
                if (type.IsGenericType)
                    type = type.GetGenericArguments()[0];
                if (p == "NULL")
                    return null;
                else if (type == typeof(DateTime))
                    return Constants.UTC.AddMilliseconds(long.Parse(p));
                else if (type == typeof(int))
                    return int.Parse(p);
                else if (type == typeof(long))
                    return long.Parse(p);
                else if (type == typeof(short))
                    return short.Parse(p);
                else if (type == typeof(byte))
                    return byte.Parse(p);
                else if (type == typeof(uint))
                    return uint.Parse(p);
                else if (type == typeof(ulong))
                    return ulong.Parse(p);
                else if (type == typeof(ushort))
                    return ushort.Parse(p);
                else if (type == typeof(double))
                    return double.Parse(p);
                else if (type == typeof(decimal))
                    return decimal.Parse(p);
                else if (type == typeof(float))
                    return float.Parse(p);
                else if (type == typeof(bool))
                    return bool.Parse(p);
                else if (type.IsEnum)
                    return Enum.Parse(type, p);
                else if (type == typeof(Guid))
                    return new Guid(p);
                else
                    return p;
            }

            public int CompareTo(sModelListCall other)
            {
                if (_url.ToLower().StartsWith(other._url.ToLower()))
                    return -1;
                else if (other._url.ToLower().StartsWith(_url.ToLower()))
                    return 1;
                return _url.CompareTo(other._url);
            }
        }

        private struct sCachedCall
        {
            private sModelListCall _listCall;
            private object[] _opars;

            public sCachedCall(sModelListCall listCall, object[] opars)
            {
                _listCall=listCall;
                _opars=opars;
            }

            public Task HandleRequest(string url, ModelRequestHandler.RequestMethods method, Hashtable formData, HttpContext context, ISecureSession session, IsValidCall securityCheck)
            {
                return _listCall.HandleRequest(url, method, formData, context, session, securityCheck, _opars);
            }
        }

        private List<sModelListCall> _calls;

        public ModelListCallHandler()
        {
            _calls = new List<sModelListCall>();
        }

        public void ClearCache()
        {
            _calls.Clear();
        }

        public Task HandleRequest(string url, ModelRequestHandler.RequestMethods method, Hashtable formData, HttpContext context, ISecureSession session, IsValidCall securityCheck,object cachedItems)
        {
            Logger.Trace("Attempting to handle {0}:{1} in the Model List Call Handler", new object[] { method, url });
            if (cachedItems==null)
                throw new CallNotFoundException();
            sCachedCall cachedCall = (sCachedCall)cachedItems;
            return cachedCall.HandleRequest(url,method,formData,context,session,securityCheck);
        }

        public bool HandlesRequest(string url, ModelRequestHandler.RequestMethods method,out object cachedItems)
        {
            cachedItems = null;
            Logger.Trace("Checking to see if {0}:{1} is handled by the model list call", new object[] { method, url });
            bool ret = false;
            if (method==ModelRequestHandler.RequestMethods.GET)
            {
                lock (_calls)
                {
                    foreach (sModelListCall call in _calls)
                    {
                        if (call.IsValid(url))
                        {
                            object[] opars;
                            if (call.ConvertParameters(url, out opars))
                            { 
                                cachedItems=new sCachedCall(call, opars);
                                ret = true;
                            }
                            break;
                        }
                    }
                }
            }
            return ret;
        }

        public void Init(List<Type> types)
        {
            lock (_calls)
            {
                _calls.Clear();
                _LoadTypes(types);
            }
        }

        private void _LoadTypes(List<Type> types){
            foreach (Type t in types)
            {
                foreach (MethodInfo mi in t.GetMethods(Constants.LOAD_METHOD_FLAGS))
                {
                    if (mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0)
                        _calls.Add(new sModelListCall((ModelListMethod)mi.GetCustomAttributes(typeof(ModelListMethod), false)[0], mi));
                }
            }
            _calls.Sort();
        }

        #if NET
        public void LoadTypes(List<Type> types){
            lock(_calls){
                _LoadTypes(types);
            }
        }
        public void UnloadTypes(List<Type> types){
            lock(_calls){
                foreach(Type t in types){
                    for(int x=0;x<_calls.Count;x++){
                        if (_calls[x].IsForType(t)){
                            _calls.RemoveAt(x);
                            x--;
                        }
                    }
                }
                _calls.Sort();
            }
        }
        #endif
    }
}
