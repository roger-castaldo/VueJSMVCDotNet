﻿using Microsoft.AspNetCore.Http;
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
    internal class ModelListCallHandler : IRequestHandler
    {
        private struct sModelListCall
        {
            private static readonly Regex _regParameter = new Regex("\\{(\\d+)\\}", RegexOptions.Compiled | RegexOptions.ECMAScript);

            private Regex _reg;
            private MethodInfo _method;
            private int[] _groupIndexes;
            private bool _isPaged;

            public sModelListCall(ModelListMethod mlm,MethodInfo mi)
            {
                _isPaged = mlm.Paged;
                string reg = "";
                _groupIndexes = null;
                if (mi.GetParameters().Length > 0)
                {
                    ParameterInfo[] pars = mi.GetParameters();
                    string[] regexs = new string[pars.Length];
                    reg = (mlm.Path + (mlm.Paged ? (mlm.Path.Contains("?") ? "&" : "?") + "PageStartIndex={" + (regexs.Length - 3).ToString() + "}&PageSize={" + (regexs.Length - 2).ToString() + "}" : "")).Replace("?", "\\?");
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
                            regexs[x] = "(-?\\d+" + (nullable ? "|NULL" : "") + ")";
                        else if (ptype == typeof(double) ||
                            ptype == typeof(decimal) ||
                            ptype == typeof(float))
                            regexs[x] = "(-?\\d+(.\\d+)?" + (nullable ? "|NULL" : "") + ")";
                        else if (ptype == typeof(bool))
                            regexs[x] = "(true|false" + (nullable ? "|NULL" : "") + ")";
                        else if (ptype.IsEnum)
                        {
                            regexs[x] = "(";
                            foreach (string str in Enum.GetNames(ptype))
                                regexs[x] += str + "|";
                            regexs[x] = regexs[x].Substring(0, regexs[x].Length - 1) + (nullable ? "|NULL" : "") + ")";
                        }
                    }
                    _groupIndexes = new int[(mlm.Paged ? pars.Length - 1 : pars.Length)];
                    MatchCollection matches = _regParameter.Matches(reg);
                    for (int x = 0; x < matches.Count; x++)
                        _groupIndexes[int.Parse(matches[x].Groups[1].Value)] = x;
                    reg = string.Format(reg, regexs);
                    reg = (reg.StartsWith("/") ? reg : "/" + reg).TrimEnd('/');
                }
                else
                    reg = mlm.Path.Replace("?", "\\?");
                _reg = new Regex(reg, RegexOptions.Compiled|RegexOptions.ECMAScript);
                _method = mi;
            }

            public bool IsValid(string url)
            {
                return _reg.IsMatch(url);
            }

            public Task HandleRequest(string url, RequestHandler.RequestMethods method, string formData, HttpContext context, ISecureSession session, IsValidCall securityCheck)
            {
                if (!securityCheck.Invoke(_method.DeclaringType, _method, session,null,url,(Hashtable)JSON.JsonDecode(formData)))
                    throw new InsecureAccessException();
                context.Response.ContentType = "text/json";
                context.Response.StatusCode= 200;
                ParameterInfo[] pars = _method.GetParameters();
                object[] opars = new object[] { };
                if (pars.Length > 0)
                {
                    opars = new object[pars.Length];
                    Match m = _reg.Match(url);
                    for (int x = 0; x < _groupIndexes.Length; x++)
                        opars[x] = _ConvertParameterValue(m.Groups[_groupIndexes[x] + 1].Value, pars[x].ParameterType);
                }
                object ret = _method.Invoke(null, opars);
                if (_isPaged)
                {
                    return context.Response.WriteAsync(JSON.JsonEncode(new Hashtable()
                        {
                            {"response",ret },
                            {"TotalPages",opars[opars.Length-1] }
                        }));
                }
                else
                    return context.Response.WriteAsync(JSON.JsonEncode(ret));
            }

            private object _ConvertParameterValue(string p, Type type)
            {
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
                else
                    return p;
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

        public Task HandleRequest(string url, RequestHandler.RequestMethods method, string formData, HttpContext context, ISecureSession session, IsValidCall securityCheck)
        {
            sModelListCall? mlc = null;
            lock (_calls)
            {
                foreach (sModelListCall call in _calls)
                {
                    if (call.IsValid(url))
                    {
                        mlc = call;
                        break;
                    }
                }
            }
            if (mlc.HasValue)
                return mlc.Value.HandleRequest(url,method,formData,context,session,securityCheck);
            throw new CallNotFoundException();
        }

        public bool HandlesRequest(string url, RequestHandler.RequestMethods method)
        {
            if (method==RequestHandler.RequestMethods.GET)
            {
                bool ret = false;
                lock (_calls)
                {
                    foreach (sModelListCall call in _calls)
                    {
                        if (call.IsValid(url))
                        {
                            ret = true;
                            break;
                        }
                    }
                }
                return ret;
            }
            return false;
        }

        public void Init(List<Type> types)
        {
            lock (_calls)
            {
                _calls.Clear();
                foreach (Type t in types)
                {
                    foreach (MethodInfo mi in t.GetMethods(Constants.LOAD_METHOD_FLAGS))
                    {
                        if (mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0)
                            _calls.Add(new sModelListCall((ModelListMethod)mi.GetCustomAttributes(typeof(ModelListMethod), false)[0], mi));
                    }
                }
            }
        }
    }
}
