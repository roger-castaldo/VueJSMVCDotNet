using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Endpoints.DataSources;
using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.Interfaces.Internal;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal class ModelRequestData(Dictionary<string, object> formData, ISecureSession? session, HttpContext httpContext,
        ILogger? log, IFormFileCollection? files, JsonDocument? rawBody, string? id, ModelsDataSource modelsDataSource) : IInternalRequestData
    {
        public JsonDocument? RawBody => rawBody;
        ISecureSession? IRequestData.Session => session;
        IEnumerable<string> IRequestData.Keys
            => formData.Keys
            .Concat(files==null||files.Count==0
            ? []
            : files.Select(f => f.Name));

        T? IRequestData.GetValue<T>(string key)
            where T : default
        {
            key = ((IRequestData)this).Keys.FirstOrDefault(k => String.Equals(k, key, StringComparison.InvariantCultureIgnoreCase))??string.Empty;
            if (string.IsNullOrEmpty(key))
                throw new KeyNotFoundException();
            if (formData.TryGetValue(key, out object? value))
            {
                try
                {
                    if (value is JsonDocument document)
                        return Utility.JsonDecode<T>(document, this);
                    else if (value is JsonNode node)
                        return Utility.JsonDecode<T>(node, this);
                    else if (value is JsonElement element)
                        return Utility.JsonDecode<T>(element, this);
                    else
                        return (T?)ConvertObjectToType(value, typeof(T));
                }
                catch (Exception)
                {
                    throw new InvalidCastException();
                }
            }
            else if (typeof(T)==typeof(IReadOnlyList<IFormFile>))
                return (T?)files?.GetFiles(key);
            else
                return (T?)files?[key];
        }
        object? IRequestData.this[Type feature]
        {
            get
            {
                if (feature==typeof(ISecureSession)
                    || feature.GetInterfaces().Contains(typeof(ISecureSession)))
                    return ((IRequestData)this).Session;
                else if (feature==typeof(ILogger))
                    return log;
                else if (feature == typeof(HttpContext))
                    return httpContext;
                else if (feature==typeof(IHeaderDictionary))
                    return httpContext.Response.Headers;
                else if (feature==typeof(IRequestCookieCollection))
                    return httpContext.Request.Cookies;
                else if (feature==typeof(IResponseCookies))
                    return httpContext.Response.Cookies;
                return (httpContext.RequestServices?.GetService(feature))??
                    (httpContext.Features?.FirstOrDefault(t => t.Key==feature).Value);
            }
        }

        string? IInternalRequestData.ModelID => id;

        object? IInternalRequestData.GetValue(Type t, string key)
        {
            try
            {
                return typeof(IRequestData).GetMethod(nameof(IRequestData.GetValue), [typeof(string)])?.MakeGenericMethod(t).Invoke(this, [key]);
            }
            catch (Exception)
            {
                throw new InvalidCastException();
            }
        }

        IModelHandler<M>? IInternalRequestData.GetModelHandlerType<M>()
            => modelsDataSource.GetModelHandlerType<M>(httpContext.RequestServices);

        ValueTask<M?> IInternalRequestData.LoadModelAsync<M>(string modelID) where M : default
            => ((IInternalRequestData)this).GetModelHandlerType<M>()?.LoadAsync(modelID) ?? throw new ArgumentNullException("Unable to locate loader");

        async ValueTask<object?> IInternalRequestData.LoadModelAsync(Type modelType, string modelID)
        {
            var loader = typeof(IInternalRequestData).GetMethod(nameof(IInternalRequestData.GetModelHandlerType))?
                .MakeGenericMethod(modelType)
                .Invoke(this, null) ?? throw new ArgumentNullException("Unable to locate loader");
            var valueTask = typeof(IModelHandler<>).GetMethod("LoadAsync")?
                .Invoke(loader,[modelID]);
            var task = (Task)typeof(ValueTask<>).GetMethod(nameof(ValueTask.AsTask))?
                .Invoke(valueTask, null)!;
            await task;
            return task.GetType().GetProperty("Result")!.GetValue(task);
        }

        private object? ConvertObjectToType(object? obj, Type expectedType)
        {
            log?.LogTrace("Attempting to convert object of type {SourceTyp} to {DestinationType}", (obj == null ? "NULL" : obj.GetType().FullName), expectedType.FullName);
            if (expectedType.Equals(typeof(object)))
                return obj;
            else if (Equals(expectedType, typeof(bool)))
                return Equals(obj, true);
            else if (obj == null)
                return null;
            else if (Equals(obj.GetType(), expectedType))
                return obj;
            else if (Equals(expectedType, typeof(string)))
                return obj.ToString();
            else if (expectedType.IsEnum)
            {
                Enum.TryParse(expectedType, obj.ToString(), out var enumValue);
                return enumValue;
            }
            else if (expectedType.Equals(typeof(Version)))
                return new Version(obj.ToString()!);
            else if (expectedType.Equals(typeof(Guid)))
                return new Guid(obj!.ToString()!);
            else if (expectedType.GetInterfaces().Contains(typeof(IDictionary)))
            {
                var keyType = expectedType.GetGenericArguments()[0];
                var valType = expectedType.GetGenericArguments()[1];
                var ret = (IDictionary)Activator.CreateInstance(expectedType)!;

                foreach (string str in ((Hashtable)obj).Keys)
                    ret.Add(ConvertObjectToType(str, keyType)!, ConvertObjectToType(((Hashtable)obj)[str], valType));
                return ret;
            }
            else if (obj is ICollection || expectedType.IsArray)
            {
                Type underlyingType;
                if (expectedType.IsGenericType)
                    underlyingType = expectedType.GetGenericArguments()[0];
                else
                    underlyingType = expectedType.GetElementType()!;
                if (obj is ICollection list)
                {
                    var ret = Array.CreateInstance(underlyingType, list.Count);
                    var idx = 0;
                    foreach (var item in list)
                    {
                        ret.SetValue(ConvertObjectToType(item, underlyingType), idx);
                        idx++;
                    }
                    if (expectedType.FullName?.StartsWith("System.Collections.Generic.List")??false)
                        return Activator.CreateInstance(expectedType, ret);
                    return ret;
                }
                else
                {
                    var ret = Array.CreateInstance(underlyingType, 1);
                    ret.SetValue(ConvertObjectToType(obj, underlyingType), 0);
                    if (expectedType.FullName?.StartsWith("System.Collections.Generic.List")??false)
                        return Activator.CreateInstance(expectedType, ret);
                }
                return Array.CreateInstance(underlyingType, 0);
            }
            else if (expectedType.FullName?.StartsWith("System.Nullable")??false)
            {
                Type underlyingType;
                if (expectedType.IsGenericType)
                    underlyingType = expectedType.GetGenericArguments()[0];
                else
                    underlyingType = expectedType.GetElementType()!;
                return ConvertObjectToType(obj, underlyingType);
            }
            else if (new List<Type>(expectedType.GetInterfaces()).Contains(typeof(IModel)))
            {
                var task = ((IInternalRequestData)this).LoadModelAsync(expectedType, ((Hashtable)obj)["id"]!.ToString()!).AsTask();
                task.Wait();
                return task.Result;
            }
            MethodInfo? conMethod = null;
            foreach (MethodInfo mi in expectedType.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                if ((mi.Name == "op_Implicit" || mi.Name == "op_Explicit")&&                        (
                            mi.ReturnType.Equals(expectedType)
                            || mi.ReturnType.Equals(typeof(Nullable<>).MakeGenericType(expectedType))
                        )
                        && mi.GetParameters().Length == 1
                        && (
                            mi.GetParameters()[0].ParameterType.Equals(obj.GetType())
                            || mi.GetParameters()[0].ParameterType.Equals(typeof(Nullable<>).MakeGenericType(obj.GetType()))
                        )
)
                {
                    conMethod = mi;
                    break;
                }
            }
            if (conMethod != null)
                return conMethod.Invoke(null, [obj]);
            try
            {
                object ret = Convert.ChangeType(obj, expectedType);
                return ret;
            }
            catch (Exception e)
            {
                log?.LogError(e, "Type conversion error: {ErrorMessage}", e.Message);
            }
            return obj;
        }
    }
}
