using Microsoft.AspNetCore.Http;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
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
                if (value==null)
                    return default;
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
            => ((IInternalRequestData)this).GetModelHandlerType<M>()?.LoadAsync(modelID) ?? throw new UnableToLocateLoaderException();

        private static object? ConvertObjectToType(object? obj, Type expectedType)
        {
            if (Equals(obj?.GetType(), expectedType))
                return obj;
            if (obj is ICollection || expectedType.IsArray)
            {
                Type underlyingType;
                if (expectedType.IsGenericType)
                    underlyingType = expectedType.GetGenericArguments()[0];
                else
                    underlyingType = expectedType.GetElementType()!;
                var result = Array.CreateInstance(underlyingType,0);
                if (obj is ICollection list)
                {
                    result = Array.CreateInstance(underlyingType, list.Count);
                    var idx = 0;
                    foreach (var item in list)
                    {
                        result.SetValue(ConvertObjectToType(item, underlyingType), idx);
                        idx++;
                    }
                    if (expectedType.FullName?.StartsWith("System.Collections.Generic.List")??false)
                        return Activator.CreateInstance(expectedType, result);
                }
                else if (expectedType.FullName?.StartsWith("System.Collections.Generic.List")??false)
                {
                    result = Array.CreateInstance(underlyingType, 1);
                    result.SetValue(ConvertObjectToType(obj, underlyingType), 0);
                    return Activator.CreateInstance(expectedType, result);
                }
                return result;
            }
            return obj;
        }
    }
}
