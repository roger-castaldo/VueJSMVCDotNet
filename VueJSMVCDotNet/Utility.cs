using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Model;
using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.JSON;

namespace VueJSMVCDotNet
{
    /*
     * This class houses some basic utility functions used by other classes to access embedded resources, search
     * for types, etc.
     */
    internal static class Utility
    {
        //houses a cache of Types found through locate type, this is used to increase performance
        private static readonly Dictionary<string, Type> _TYPE_CACHE = [];
        //houses a cache of Type instances through locate type instances, this is used to increate preformance
        private static readonly Dictionary<string, IEnumerable<Type>> _INSTANCES_CACHE = [];
        //houses the assembly load contexts for types
        private static readonly Dictionary<string, IEnumerable<Type>> _LOAD_CONTEXT_TYPE_SOURCES = [];

        internal static IModel SetModelValues(ModelRequestData data, IModel model, bool isNew, ILogger? log)
        {
            data.Keys.Where(key => !string.Equals(key, "id", StringComparison.InvariantCulture))
            .Select(key => model.GetType().GetProperty(key))
            .Where(pi => pi != null && pi.CanWrite
                        && (pi.GetCustomAttribute<ReadOnlyModelPropertyAttribute>(true)==null||isNew)
            )
            .ForEach(pi =>
            {
                log?.LogTrace("Attempting to convert the value supplied for property {FullName}.{Name} to {PropertyType}", model.GetType().FullName, pi!.Name, pi.PropertyType);
                pi!.SetValue(model, data.GetValue(pi.PropertyType, pi.Name));
            });
            return model;
        }

        public static IEnumerable<Type> LocateTypeInstances(Type parent, AssemblyLoadContext alc, ILogger? log)
        {
            log?.LogTrace("Locating Instance types of {FullName} in the Load Context {Name}", parent.FullName, alc.Name);
            return LocateTypeInstances(parent, alc.Assemblies, log)
                .ForEach(t => MarkTypeSource(alc.Name!, t, log));
        }

        private static IEnumerable<Type> LocateTypeInstances(Type parent, IEnumerable<Assembly> assemblies, ILogger? log)
            => assemblies
            .Where(ass => !Equals(ass.GetName().Name, "mscorlib")
                && !ass.GetName().Name!.StartsWith("System.")
                && !Equals(ass.GetName().Name, "System")
                && !ass.GetName().Name!.StartsWith("Microsoft.")
            )
            .SelectMany(ass =>
                GetLoadableTypes(ass, log)
                .Where(t => t.IsSubclassOf(parent) || (parent.IsInterface && t.GetInterfaces().Contains(parent)))
            );

        private static IEnumerable<Type> GetLoadableTypes(Assembly ass, ILogger? log)
        {
            log?.LogTrace("Extracting Loadable types from assembly: {FullName}", ass.FullName);
            IEnumerable<Type> ret;
            try
            {
                ret = ass.GetTypes();
            }
            catch (ReflectionTypeLoadException rtle)
            {
                log?.LogError(rtle, "Reflection Load Exception from getting loadable types: {Message}", rtle.Message);
                ret = rtle.Types!;
            }
            catch (Exception e)
            {
                log?.LogError(e, "General Error attempting to load types from assembly: {Message}", e.Message);
                if (e.Message != "The invoked member is not supported in a dynamic assembly."
                            && !e.Message.StartsWith("Unable to load one or more of the requested types."))
                    throw;
                else
                    ret = [];
            }
            return ret;
        }

        private static void MarkTypeSource(string contextName, Type type, ILogger? log)
        {
            log?.LogTrace("Marking the Assembly Load Context of {ContextName} for the type {FullName}", contextName, type.FullName);
            lock (_LOAD_CONTEXT_TYPE_SOURCES)
            {
                IEnumerable<Type> types = [];
                if (_LOAD_CONTEXT_TYPE_SOURCES.TryGetValue(contextName, out var value))
                {
                    types = value;
                    _LOAD_CONTEXT_TYPE_SOURCES.Remove(contextName);
                }
                if (!types.Contains(type))
                    types=types.Append(type);
                _LOAD_CONTEXT_TYPE_SOURCES.Add(contextName, types);
            }
        }

        internal static IEnumerable<Type> UnloadAssemblyContext(string? contextName)
        {
            if (contextName == null)
                return [];
            IEnumerable<Type> ret = [];
            lock (_LOAD_CONTEXT_TYPE_SOURCES)
            {
                if (_LOAD_CONTEXT_TYPE_SOURCES.TryGetValue(contextName, out var value))
                {
                    ret = value;
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
            lock (_LOAD_CONTEXT_TYPE_SOURCES)
            {
                _LOAD_CONTEXT_TYPE_SOURCES.Clear();
            }
        }

        internal static string GetModelUrlRoot(Type modelType)
            => GetModelUrlRoot(modelType, null);

        internal static string GetModelUrlRoot(Type modelType, string? urlBase)
            => $"{(urlBase??"")}{modelType.GetCustomAttribute<ModelRouteAttribute>(false)?.Path}".Replace("//", "/");

        private static readonly Regex _regNoCache = new("[?&]_=(\\d+)$", RegexOptions.Compiled | RegexOptions.ECMAScript, TimeSpan.FromMilliseconds(500));

        public static string CleanURL(Uri url)
            => _regNoCache.Replace(url.PathAndQuery, "");

        public static Uri BuildURL(HttpContext context, string? urlBase)
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
            ExtractUnderlyingType(type, out var isArray, out _, out _);
            return isArray;
        }

        public static Type ExtractUnderlyingType(Type type, out bool isArray, out bool isNullable, out bool isTask)
        {
            isArray = false;
            isNullable = false;
            isTask= false;
            if (type == typeof(Task) || (type.IsGenericType && type.GetGenericTypeDefinition()==typeof(Task<>)))
            {
                isTask=true;
                if (type.IsGenericType)
                    type=type.GetGenericArguments()[0];
                else
                    return typeof(void);
            }
            if (type.IsArray)
            {
                isArray=true;
                type=type.GetElementType()!;
            }
            else if (type.IsGenericType &&
                (
                type.GetGenericTypeDefinition()==typeof(IEnumerable<>) ||
                Array.Exists(type.GetGenericTypeDefinition().GetInterfaces(), (t) => t.IsGenericType && t.GetGenericTypeDefinition()==typeof(IEnumerable<>))
             ))
            {
                isArray=true;
                isNullable=true;
                type=type.GetGenericArguments()[0];
            }
            if (type.FullName!.StartsWith("System.Nullable"))
            {
                isNullable=true;
                if (type.IsGenericType)
                    type=type.GetGenericArguments()[0];
                else
                    type=type.GetElementType()!;
            }
            return type;
        }

        internal static string GetTypeString(Type propertyType, bool notNullTagged)
        {
            var ptype = ExtractUnderlyingType(propertyType, out var isArray, out var isNullable, out _);
            if (isArray)
                return $"{GetTypeString(ptype, false)}[]{(ptype==typeof(byte) && !notNullTagged ? "?" : "")}";
            else if (isNullable)
                return $"{GetTypeString(ptype, true)}?";
            else if (ptype.IsEnum)
                return "Enum";
            else if (ptype.IsSubclassOf(typeof(Exception)))
                return "System.Exception";
            else if (ptype==typeof(IFormFile))
                return "IFormFile"+(!notNullTagged ? "?" : "");
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
            var type = ExtractUnderlyingType(propertyType, out _, out _, out _);
            if (type.IsEnum)
                return $"[{string.Join(',', Enum.GetNames(type).Select(s => $"'{s}'"))}]";
            else
                return "undefined";
        }

        internal static string? TranslatePath(IFileProvider fileProvider, string? baseURL, string path)
        {
            string[] split = path.TrimStart('/').Split('/');
            string? curPath = "";
            foreach (string sub in split)
            {
                if (sub=="..")
                {
                    if (curPath.Contains(Path.DirectorySeparatorChar.ToString()))
                        curPath=curPath[..curPath.LastIndexOf(Path.DirectorySeparatorChar)];
                }
                else if (sub!="" && sub!=".")
                {
                    bool changed = false;
                    foreach (IFileInfo ifi in fileProvider.GetDirectoryContents(curPath))
                    {
                        if (ifi.IsDirectory && string.Equals(ifi.Name, sub.Trim(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            curPath=$"{curPath}{(!string.IsNullOrEmpty(curPath) ? Path.DirectorySeparatorChar.ToString() : "")}{ifi.Name}";
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

        private static JsonSerializerOptions ProduceJsonOptions(ILogger? log, IRequestData? requestData = null)
        {
            var result = new JsonSerializerOptions
            {
                WriteIndented=false
            };
            result.Converters.Add(new JsonStringEnumConverter());
            result.Converters.Add(new DateTimeConverter());
            result.Converters.Add(new GuidConverter());
            result.Converters.Add(new IPAddressConverter());
            result.Converters.Add(new DecimalConverter());
            result.Converters.Add(new ModelConverterFactory(requestData));
            return result;
        }

        public static string JsonEncode(object? value, ILogger? log)
        {
            if (value==null)
                return "null";
            return JsonSerializer.Serialize(value, value.GetType(), options: ProduceJsonOptions(log));
        }

        public static T? JsonDecode<T>(JsonDocument document, IRequestData requestData, ILogger? log)
            => JsonSerializer.Deserialize<T>(document, options: ProduceJsonOptions(log, requestData));

        public static T? JsonDecode<T>(JsonNode node, IRequestData requestData, ILogger? log)
            => JsonSerializer.Deserialize<T>(node, options: ProduceJsonOptions(log, requestData));

        public static T? JsonDecode<T>(JsonElement element, IRequestData requestData, ILogger? log)
            => JsonSerializer.Deserialize<T>(element, options: ProduceJsonOptions(log, requestData));
        #endregion

        public static string? SantizeLogValue(string? value) => value?.Replace('\r', '_').Replace('\n', '_');
    }
}
