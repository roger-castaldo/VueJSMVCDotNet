using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Model;
using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.JSON;
using System.Collections;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace VueJSMVCDotNet
{
    /*
     * This class houses some basic utility functions used by other classes to access embedded resources, search
     * for types, etc.
     */
    internal static class Utility
    {
        //houses a cache of Types found through locate type, this is used to increase performance
        private static readonly Dictionary<string, Type> _TYPE_CACHE = new();
        //houses a cache of Type instances through locate type instances, this is used to increate preformance
        private static readonly Dictionary<string, List<Type>> _INSTANCES_CACHE = new();
        //houses the assembly load contexts for types
        private static readonly Dictionary<string, List<Type>> _LOAD_CONTEXT_TYPE_SOURCES = new();

        internal static void SetModelValues(ModelRequestData data, ref IModel model, bool isNew,ILogger log)
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
                                log?.LogTrace("Attempting to convert the value supplied for property {}.{} to {}", model.GetType().FullName, pi.Name, pi.PropertyType);
                                pi.SetValue(model,data.GetValue(pi.PropertyType,str));
                            }
                        }
                    }
                }
            }
        }

        public static List<Type> LocateTypeInstances(Type parent, AssemblyLoadContext alc, ILogger log) {
            log?.LogTrace("Locating Instance types of {} in the Load Context {}",  parent.FullName, alc.Name);
            List<Type> ret = LocateTypeInstances(parent, alc.Assemblies,log);
            foreach (Type t in ret) {
                MarkTypeSource(alc.Name, t,log);
            }
            return ret;
        }

        private static List<Type> LocateTypeInstances(Type parent, IEnumerable<Assembly> assemblies, ILogger log)
        {
            List<Type> ret = new();
            foreach (Assembly ass in assemblies)
            {
                if (ass.GetName().Name != "mscorlib" && !ass.GetName().Name.StartsWith("System.") && ass.GetName().Name != "System" && !ass.GetName().Name.StartsWith("Microsoft"))
                {
                    foreach (Type t in GetLoadableTypes(ass, log))
                    {
                        if (t.IsSubclassOf(parent) || (parent.IsInterface && new List<Type>(t.GetInterfaces()).Contains(parent))) {
                            ret.Add(t);
                        }
                    }
                }
            }
            log?.LogTrace("Located {} instances of type {} from the given assemblies", ret.Count, parent.FullName);
            return ret;
        }

        private static Type[] GetLoadableTypes(Assembly ass, ILogger log)
        {
            log?.LogTrace("Extracting Loadable types from assembly: {}", ass.FullName);
            Type[] ret;
            try
            {
                ret = ass.GetTypes();
            }
            catch (ReflectionTypeLoadException rtle)
            {
                log?.LogError("Reflection Load Exception from getting loadable types: {}",rtle.Message);
                ret = rtle.Types;
            }
            catch (Exception e)
            {
                log?.LogError("General Error attempting to load types from assembly: {}",e.Message);
                if (e.Message != "The invoked member is not supported in a dynamic assembly."
                            && !e.Message.StartsWith("Unable to load one or more of the requested types."))
                    throw;
                else
                    ret = Array.Empty<Type>();
            }
            return ret;
        }

        private static void MarkTypeSource(string contextName, Type type, ILogger log) {
            log?.LogTrace("Marking the Assembly Load Context of {} for the type {}", contextName, type.FullName);
            lock (_LOAD_CONTEXT_TYPE_SOURCES)
            {
                List<Type> types = new();
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
        internal static void ClearCaches(ILogger log)
        {
            log?.LogTrace("Clearing cached types from loaded contexts");
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

        private static readonly Regex _regNoCache = new("[?&]_=(\\d+)$", RegexOptions.Compiled | RegexOptions.ECMAScript,TimeSpan.FromMilliseconds(500));

        public static string CleanURL(Uri url)
        {
            return _regNoCache.Replace(url.PathAndQuery, "");
        }

        public static Uri BuildURL(HttpContext context, string urlBase)
        {
            UriBuilder builder = new(
                context.Request.Scheme,
                context.Request.Host.Host,
                (context.Request.Host.Port??(context.Request.IsHttps ? 443 : 80)),
                (urlBase==null ? context.Request.Path.ToString() : context.Request.Path.ToString().Replace(urlBase, "/"))
            );
            if (context.Request.QueryString.HasValue)
                builder.Query = context.Request.QueryString.Value[1..];
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
            else if (propertyType==typeof(IFormFile))
                return "IFormFile"+(!notNullTagged ? "?" : "");
            else if (propertyType==typeof(IReadOnlyList<IFormFile>))
                return "IFormFile[]";
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
                StringBuilder sb = new();
                sb.Append('[');
                bool isFirst = true;
                foreach (string str in Enum.GetNames(propertyType))
                {
                    sb.Append($"{(isFirst ? "" : ",")}'{str}'");
                    isFirst = false;
                }
                sb.Append(']');
                return sb.ToString();
            }
            else
                return "undefined";
        }

        internal static string TranslatePath(IFileProvider fileProvider, string baseURL, string path)
        {
            string[] split = path.TrimStart('/').Split('/');
            string curPath = "";
            foreach (string sub in split)
            {
                if (sub=="..")
                {
                    if (curPath.Contains(Path.DirectorySeparatorChar.ToString()))
                        curPath=curPath[..curPath.LastIndexOf(Path.DirectorySeparatorChar)];
                } else if (sub!="" && sub!=".")
                {
                    bool changed = false;
                    foreach (IFileInfo ifi in fileProvider.GetDirectoryContents(curPath))
                    {
                        if (ifi.IsDirectory && string.Equals(ifi.Name,sub.Trim(),StringComparison.InvariantCultureIgnoreCase))
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
                return TranslatePath(fileProvider, null, path[baseURL.Length..]);
            return (curPath==null || curPath=="" ? null : curPath);
        }

        #region JSON

        private static JsonSerializerOptions ProduceJsonOptions(ILogger log,IRequestData requestData = null)
        {
            var result = new JsonSerializerOptions
            {
                WriteIndented=false
            };
            result.Converters.Add(new DateTimeConverter());
            result.Converters.Add(new GuidConverter());
            result.Converters.Add(new IPAddressConverter());
            result.Converters.Add(new DecimalConverter());
            result.Converters.Add(new ModelConverterFactory(requestData,log));
            result.Converters.Add(new EnumConverterFactory());
            return result;
        }

        public static string JsonEncode(object value, ILogger log)
        {
            if (value==null)
                return "null";
            return JsonSerializer.Serialize(value, value.GetType(), options: ProduceJsonOptions(log));
        }

        public static T JsonDecode<T>(JsonDocument document, IRequestData requestData, ILogger log)
        {
            return (T)JsonSerializer.Deserialize(document, typeof(T), options: ProduceJsonOptions(log,requestData));
        }

        public static T JsonDecode<T>(JsonNode node, IRequestData requestData, ILogger log)
        {
            return (T)JsonSerializer.Deserialize(node, typeof(T), options: ProduceJsonOptions(log, requestData));
        }

        public static T JsonDecode<T>(JsonElement element, IRequestData requestData, ILogger log)
        {
            return (T)JsonSerializer.Deserialize(element, typeof(T), options: ProduceJsonOptions(log, requestData));
        }
        #endregion

        public static string? SantizeLogValue(string? value)
        {
            return value?.Replace('\r', '_').Replace('\n', '_');
        }
    }
}
