﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Handlers.Model;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.JSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.ModelRequestHandlerBase;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    /*
     * This class houses some basic utility functions used by other classes to access embedded resources, search
     * for types, etc.
     */
    internal static class Utility
    {
        //houses a cache of Types found through locate type, this is used to increase performance
        private static Dictionary<string, Type> _TYPE_CACHE = new();
        //houses a cache of Type instances through locate type instances, this is used to increate preformance
        private static Dictionary<string, List<Type>> _INSTANCES_CACHE = new();
        //houses the assembly load contexts for types
        private static Dictionary<string, List<Type>> _LOAD_CONTEXT_TYPE_SOURCES = new();

        internal static void SetModelValues(ModelRequestData data, ref IModel model, bool isNew)
        {
            foreach (string str in data.Keys)
            {
                if (str != "id")
                {
                    PropertyInfo pi = model.GetType().GetProperty(str);
                    if (pi != null)
                    {
                        if (pi.CanWrite)
                        {
                            if (pi.GetCustomAttributes(typeof(ReadOnlyModelProperty), true).Length==0 || isNew)
                            {
                                Logger.Trace("Attempting to convert the value supplied for property {0}.{1} to {2}", new object[] { model.GetType().FullName, pi.Name, pi.PropertyType });
                                pi.SetValue(model,data.GetValue(pi.PropertyType,str));
                            }
                        }
                    }
                }
            }
        }

        public static List<Type> LocateTypeInstances(Type parent, AssemblyLoadContext alc) {
            Logger.Trace("Locating Instance types of {0} in the Load Context {1}", new object[] { parent.FullName, alc.Name });
            List<Type> ret = _LocateTypeInstances(parent, alc.Assemblies);
            foreach (Type t in ret) {
                _MarkTypeSource(alc.Name, t);
            }
            return ret;
        }

        private static List<Type> _LocateTypeInstances(Type parent, IEnumerable<Assembly> assemblies)
        {
            List<Type> ret = new List<Type>();
            foreach (Assembly ass in assemblies)
            {
                if (ass.GetName().Name != "mscorlib" && !ass.GetName().Name.StartsWith("System.") && ass.GetName().Name != "System" && !ass.GetName().Name.StartsWith("Microsoft"))
                {
                    foreach (Type t in _GetLoadableTypes(ass))
                    {
                        if (t.IsSubclassOf(parent) || (parent.IsInterface && new List<Type>(t.GetInterfaces()).Contains(parent))) {
                            ret.Add(t);
                        }
                    }
                }
            }
            Logger.Trace("Located {0} instances of type {1} from the given assemblies", new object[] { ret.Count, parent.FullName });
            return ret;
        }

        private static Type[] _GetLoadableTypes(Assembly ass)
        {
            Logger.Trace("Extracting Loadable types from assembly: {0}", new object[] { ass.FullName });
            Type[] ret;
            try
            {
                ret = ass.GetTypes();
            }
            catch (ReflectionTypeLoadException rtle)
            {
                Logger.Error(rtle.Message);
                ret = rtle.Types;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                if (e.Message != "The invoked member is not supported in a dynamic assembly."
                            && !e.Message.StartsWith("Unable to load one or more of the requested types."))
                    throw;
                else
                    ret = new Type[0];
            }
            return ret;
        }

        private static void _MarkTypeSource(string contextName, Type type) {
            Logger.Trace("Marking the Assembly Load Context of {0} for the type {1}", new object[] { contextName, type.FullName });
            lock (_LOAD_CONTEXT_TYPE_SOURCES)
            {
                List<Type> types = new List<Type>();
                if (_LOAD_CONTEXT_TYPE_SOURCES.ContainsKey(contextName)) {
                    types = _LOAD_CONTEXT_TYPE_SOURCES[contextName];
                    _LOAD_CONTEXT_TYPE_SOURCES.Remove(contextName);
                }
                if (!types.Contains(type)) {
                    types.Add(type);
                }
                _LOAD_CONTEXT_TYPE_SOURCES.Add(contextName, types);
            }
        }

        internal static List<Type> UnloadAssemblyContext(string contextName) {
            List<Type> ret = null;
            lock (_LOAD_CONTEXT_TYPE_SOURCES) {
                if (_LOAD_CONTEXT_TYPE_SOURCES.ContainsKey(contextName)) {
                    ret = _LOAD_CONTEXT_TYPE_SOURCES[contextName];
                    _LOAD_CONTEXT_TYPE_SOURCES.Remove(contextName);
                }
            }
            return ret;
        }
        internal static void ClearCaches()
        {
            Logger.Trace("Clearing cached types from loaded contexts");
            lock (_INSTANCES_CACHE)
            {
                _INSTANCES_CACHE.Clear();
            }
            lock (_TYPE_CACHE)
            {
                _TYPE_CACHE.Clear();
            }
            lock (_LOAD_CONTEXT_TYPE_SOURCES) {
                _LOAD_CONTEXT_TYPE_SOURCES.Clear();
            }
        }

        internal static string GetModelUrlRoot(Type modelType)
        {
            return GetModelUrlRoot(modelType, null);
        }

        internal static string GetModelUrlRoot(Type modelType, string urlBase)
        {
            string urlRoot = (urlBase??"");
            foreach (ModelRoute mr in modelType.GetCustomAttributes(typeof(ModelRoute), false).Cast<ModelRoute>())
            {
                urlRoot += mr.Path;
                break;
            }
            return urlRoot.Replace("//", "/");
        }

        private static readonly Regex _regNoCache = new Regex("[?&]_=(\\d+)$", RegexOptions.Compiled | RegexOptions.ECMAScript);

        public static string CleanURL(Uri url)
        {
            return _regNoCache.Replace(url.PathAndQuery, "");
        }

        public static Uri BuildURL(HttpContext context, string urlBase)
        {
            UriBuilder builder = new UriBuilder(
                context.Request.Scheme,
                context.Request.Host.Host,
                (context.Request.Host.Port??(context.Request.IsHttps ? 443 : 80)),
                (urlBase==null ? context.Request.Path.ToString() : context.Request.Path.ToString().Replace(urlBase, "/"))
            );
            if (context.Request.QueryString.HasValue)
                builder.Query = context.Request.QueryString.Value.Substring(1);
            return builder.Uri;
        }

        public static bool IsArrayType(Type type)
        {
            return type.IsArray ||
                (type.IsGenericType && new List<Type>(type.GetGenericTypeDefinition().GetInterfaces()).Contains(typeof(IEnumerable)));
        }

        internal static string GetTypeString(Type propertyType, bool notNullTagged)
        {
            if (propertyType.IsArray)
                return GetTypeString(propertyType.GetElementType(), false) + "[]"+(propertyType.GetElementType() == typeof(Byte) && !notNullTagged ? "?" : "");
            else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                return GetTypeString(propertyType.GetGenericArguments()[0], false) + "[]";
            else if (propertyType.FullName.StartsWith("System.Nullable"))
            {
                if (propertyType.IsGenericType)
                    return GetTypeString(propertyType.GetGenericArguments()[0], true)+"?";
                else
                    return GetTypeString(propertyType.GetElementType(), true)+"?";
            }
            else if (propertyType.IsEnum)
                return "Enum";
            else if (propertyType.IsSubclassOf(typeof(Exception)))
                return "System.Exception";
            else
            {
                switch (propertyType.FullName)
                {
                    case "System.String":
                    case "System.Net.IPAddress":
                    case "System.Version":
                    case "System.Exception":
                        return propertyType.FullName +(!notNullTagged ? "?" : "");
                    case "System.Char":
                    case "System.Int16":
                    case "System.Int32":
                    case "System.Int64":
                    case "System.SByte":
                    case "System.Single":
                    case "System.Decimal":
                    case "System.Double":
                    case "System.UInt16":
                    case "System.UInt32":
                    case "System.UInt64":
                    case "System.Byte":
                    case "System.Boolean":
                    case "System.DateTime":
                    case "System.Guid":
                        return propertyType.FullName;
                }
            }
            return "System.Object" + (!notNullTagged ? "?" : "");
        }

        internal static string GetEnumList(Type propertyType)
        {
            if (propertyType.IsArray)
                return GetEnumList(propertyType.GetElementType());
            else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                return GetEnumList(propertyType.GetGenericArguments()[0]);
            else if (propertyType.FullName.StartsWith("System.Nullable"))
            {
                if (propertyType.IsGenericType)
                    return GetEnumList(propertyType.GetGenericArguments()[0]);
                else
                    return GetEnumList(propertyType.GetElementType());
            }
            if (propertyType.IsEnum)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[");
                bool isFirst = true;
                foreach (string str in Enum.GetNames(propertyType))
                {
                    sb.AppendFormat("{1}'{0}'", new object[] {
                        str,
                        (isFirst?"":",")
                    });
                    isFirst = false;
                }
                sb.Append("]");
                return sb.ToString();
            }
            else
                return "undefined";
        }

        internal static string TranslatePath(IFileProvider fileProvider, string baseURL, string path)
        {
            string[] split = path.ToLower().Split('/');
            string curPath = "";
            foreach (string sub in split)
            {
                if (sub=="..")
                {
                    if (curPath.Contains(Path.DirectorySeparatorChar.ToString()))
                        curPath=curPath.Substring(0, curPath.LastIndexOf(Path.DirectorySeparatorChar));
                } else if (sub!="" && sub!=".")
                {
                    bool changed = false;
                    foreach (IFileInfo ifi in fileProvider.GetDirectoryContents(curPath))
                    {
                        if (ifi.IsDirectory && ifi.Name.ToLower()==sub.Trim())
                        {
                            curPath+=(curPath=="" ? "" : Path.DirectorySeparatorChar.ToString())+ifi.Name;
                            changed=true;
                            break;
                        }
                    }
                    if (!changed)
                    {
                        curPath=null;
                        break;
                    }
                }
            }
            if (curPath==null && baseURL!=null)
                return TranslatePath(fileProvider, null, path.Substring(baseURL.Length));
            return (curPath==null || curPath=="" ? null : curPath);
        }

        #region JSON

        private static JsonSerializerOptions _ProduceJsonOptions(ISecureSession session = null)
        {
            var result = new JsonSerializerOptions();
            result.WriteIndented=false;
            result.Converters.Add(new DateTimeConverter());
            result.Converters.Add(new GuidConverter());
            result.Converters.Add(new IPAddressConverter());
            result.Converters.Add(new DecimalConverter());
            result.Converters.Add(new ModelConverterFactory(session));
            result.Converters.Add(new EnumConverterFactory());
            return result;
        }

        public static string JsonEncode(object value)
        {
            if (value==null)
                return "null";
            return JsonSerializer.Serialize(value, value.GetType(), options: _ProduceJsonOptions());
        }

        public static T JsonDecode<T>(JsonDocument document, ISecureSession session)
        {
            return (T)JsonSerializer.Deserialize(document, typeof(T), options: _ProduceJsonOptions(session));
        }

        public static T JsonDecode<T>(JsonNode node, ISecureSession session)
        {
            return (T)JsonSerializer.Deserialize(node, typeof(T), options: _ProduceJsonOptions(session));
        }

        public static T JsonDecode<T>(JsonElement element, ISecureSession session)
        {
            return (T)JsonSerializer.Deserialize(element, typeof(T), options: _ProduceJsonOptions(session));
        }
        #endregion
    }
}
