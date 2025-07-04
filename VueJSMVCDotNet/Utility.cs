using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using VueJSMVCDotNet.Extensions;
using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.Interfaces.Internal;
using VueJSMVCDotNet.JSON;

namespace VueJSMVCDotNet
{
    /*
     * This class houses some basic utility functions used by other classes to access embedded resources, search
     * for types, etc.
     */
    internal static class Utility
    {
        //houses the assembly load contexts for types
        private static readonly Dictionary<string, IEnumerable<Type>> _LOAD_CONTEXT_TYPE_SOURCES = [];

        public static IEnumerable<(Type HandlerType, Type ModelType)> LocateModelHandlers(AssemblyLoadContext alc, ILogger? log)
        {
            log?.LogTrace("Locating Instance types of {FullName} in the Load Context {Name}", typeof(IModelHandler<>).FullName, alc.Name);
            return LocateTypeInstances(typeof(IModelHandler<>), alc.Assemblies, log)
                .ForEach(t => MarkTypeSource(alc.Name!, t, log))
                .Select(handlerType => (handlerType, handlerType.GetInterfaces().First(t => t.IsGenericType && Equals(t.GetGenericTypeDefinition(), typeof(IModelHandler<>))).GetGenericArguments()[0]));
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
                .Where(t =>
                    t.IsSubclassOf(parent) ||
                    (
                        parent.IsInterface
                        && Array.Exists(t.GetInterfaces(), (t) =>
                            Equals(t, parent) ||
                            (
                                parent.IsGenericType &&
                                t.IsGenericType &&
                                Equals(t.GetGenericTypeDefinition(), parent)
                            )
                        )
                    )
                )
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
            return ret.Where(t => t!=null);
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

        public static bool IsArrayType(Type type)
        {
            (_, var isArray, _, _, _) = ExtractUnderlyingType(type);
            return isArray;
        }

        public static (Type type,bool isArray,bool isNullable,bool isTask,bool isValueTask) ExtractUnderlyingType(Type type)
        {
            var isArray = false;
            var isNullable = false;
            var isTask= false;
            var isValueTask = false;
            if (type == typeof(Task) || (type.IsGenericType && type.GetGenericTypeDefinition()==typeof(Task<>)))
            {
                isTask=true;
                if (type.IsGenericType)
                    type=type.GetGenericArguments()[0];
                else
                    return (typeof(void), isArray, isNullable, isTask, isValueTask);
            }else if (type == typeof(ValueTask) || (type.IsGenericType && type.GetGenericTypeDefinition()==typeof(ValueTask<>)))
            {
                isValueTask=true;
                if (type.IsGenericType)
                    type=type.GetGenericArguments()[0];
                else
                    return (typeof(void), isArray, isNullable, isTask, isValueTask);
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
            return (type, isArray, isNullable, isTask, isValueTask);
        }

        internal static string GetTypeString(Type propertyType, bool notNullTagged)
        {
            (var ptype, var isArray, var isNullable, _, _) = ExtractUnderlyingType(propertyType);
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
            (var type, _, _, _, _) = ExtractUnderlyingType(propertyType);
            if (type.IsEnum)
                return $"[{string.Join(',', Enum.GetNames(type).Select(s => $"'{s}'"))}]";
            else
                return "undefined";
        }

        internal static string? TranslatePath(IFileProvider fileProvider, string path)
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
            return (curPath==null || curPath=="" ? null : curPath);
        }

        #region JSON

        private static JsonSerializerOptions ProduceJsonOptions(IInternalRequestData? requestData = null)
        {
            var result = new JsonSerializerOptions
            {
                WriteIndented=false
            };
            result.Converters.Add(new JsonStringEnumConverter());
            result.Converters.Add(new GuidConverter());
            result.Converters.Add(new IPAddressConverter());
            result.Converters.Add(new ModelConverterFactory(requestData));
            return result;
        }

        public static string JsonEncode(object? value, IInternalRequestData? requestData)
        {
            if (value==null)
                return "null";
            return JsonSerializer.Serialize(value, value.GetType(), options: ProduceJsonOptions(requestData));
        }

        public static async ValueTask JsonEncode<T>(HttpContext context, Task<(T? result, IInternalRequestData requestData)> task)
        {
            var data = await task;
            context.Response.ContentType= "application/json";
            context.Response.StatusCode= 200;
            await context.Response.WriteAsync(JsonEncode(data.result, data.requestData));
        }

        public static T? JsonDecode<T>(JsonDocument document, IInternalRequestData requestData)
            => JsonSerializer.Deserialize<T>(document, options: ProduceJsonOptions(requestData));

        public static T? JsonDecode<T>(JsonNode node, IInternalRequestData requestData)
            => JsonSerializer.Deserialize<T>(node, options: ProduceJsonOptions(requestData));

        public static T? JsonDecode<T>(JsonElement element, IInternalRequestData requestData)
            => JsonSerializer.Deserialize<T>(element, options: ProduceJsonOptions(requestData));
        #endregion
    }
}
