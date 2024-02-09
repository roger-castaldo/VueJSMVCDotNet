using System.Text.Json.Nodes;
using System.Text.Json;
using VueJSMVCDotNet.Interfaces;
using System.Collections;
using VueJSMVCDotNet.Attributes;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class ModelRequestData : IRequestData
    {
        private readonly ILogger log;
        private readonly Dictionary<string, object> formData;
        private readonly IServiceProvider services;
        private readonly IFeatureCollection features;
        private readonly IFormFileCollection files;

        public ISecureSession Session { get; private init; }
        public IEnumerable<string> Keys => formData.Keys
            .Concat(files==null||files.Count==0 
            ? Array.Empty<string>()
            : files.Select(f=>f.Name));

        public T GetValue<T>(string key)
        {
            key = Keys.FirstOrDefault(k => String.Equals(k, key, StringComparison.InvariantCultureIgnoreCase));
            if (key==null)
                throw new KeyNotFoundException();
            if (formData.ContainsKey(key))
            {
                var obj = formData[key];
                try
                {
                    if (obj is JsonDocument document)
                        return Utility.JsonDecode<T>(document, this, log);
                    else if (obj is JsonNode node)
                        return Utility.JsonDecode<T>(node, this, log);
                    else if (obj is JsonElement element)
                        return Utility.JsonDecode<T>(element, this, log);
                    else
                        return (T)ConvertObjectToType(obj, typeof(T));
                }
                catch (Exception)
                {
                    throw new InvalidCastException();
                }
            } else if (typeof(T)==typeof(IReadOnlyList<IFormFile>))
                return (T)files.GetFiles(key);
            else
                return (T)files[key];
        }

        internal object GetValue(Type t,string key) {
            try
            {
                return GetType().GetMethod("GetValue").MakeGenericMethod(new Type[] { t }).Invoke(this, new object[] { key });
            }catch (Exception)
            {
                throw new InvalidCastException();
            }
        }

        public object this[Type feature]
        {
            get {
                return (services?.GetService(feature))??
                    (features!=null ? (features.Any(t=>t.Key==feature) ? features.First(t=>t.Key==feature).Value : null) : null); 
            }
        }

        public ModelRequestData(Dictionary<string, object> formData, ISecureSession session, IServiceProvider services, IFeatureCollection features, ILogger log, IFormFileCollection files)
        {
            this.formData = formData;
            this.services=services;
            this.features=features;
            this.log=log;
            this.files=files;
            Session = session;
        }

        private object ConvertObjectToType(object obj, Type expectedType)
        {
            log?.LogTrace("Attempting to convert object of type {} to {}", (obj == null ? "NULL" : obj.GetType().FullName), expectedType.FullName);
            if (expectedType.Equals(typeof(object)))
                return obj;
            if (expectedType.Equals(typeof(bool)) && (obj == null))
                return false;
            if (obj == null)
                return null;
            if (obj.GetType().Equals(expectedType))
                return obj;
            if (expectedType.Equals(typeof(string)))
                return obj.ToString();
            if (expectedType.IsEnum)
                return Enum.Parse(expectedType, obj.ToString());
            if (expectedType.Equals(typeof(Version)))
                return new Version(obj.ToString());
            if (expectedType.Equals(typeof(Guid)))
                return new Guid(obj.ToString());
            if (expectedType.GetInterfaces().Contains(typeof(IDictionary)))
            {
                object ret = expectedType.GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>());
                Type keyType = expectedType.GetGenericArguments()[0];
                Type valType = expectedType.GetGenericArguments()[1];
                foreach (string str in ((Hashtable)obj).Keys)
                {
                    ((IDictionary)ret).Add(ConvertObjectToType(str, keyType), ConvertObjectToType(((Hashtable)obj)[str], valType));
                }
                return ret;
            }
            if (obj is ICollection || expectedType.IsArray)
            {
                Type underlyingType;
                if (expectedType.IsGenericType)
                    underlyingType = expectedType.GetGenericArguments()[0];
                else
                    underlyingType = expectedType.GetElementType();
                Array ret = Array.CreateInstance(underlyingType, 0);
                if (obj is ICollection list)
                {
                    ret = Array.CreateInstance(underlyingType,list.Count);
                    var idx = 0;
                    foreach (var item in list)
                    {
                        ret.SetValue(ConvertObjectToType(item, underlyingType), idx);
                        idx++;
                    }   
                }
                else {
                    ret = Array.CreateInstance(underlyingType, 1);
                    ret.SetValue(ConvertObjectToType(obj, underlyingType), 0);
                }
                if (expectedType.FullName.StartsWith("System.Collections.Generic.List"))
                    return expectedType.GetConstructor(new Type[] { ret.GetType() }).Invoke(new object[] { ret });
                return ret;
            }
            if (expectedType.FullName.StartsWith("System.Nullable"))
            {
                Type underlyingType;
                if (expectedType.IsGenericType)
                    underlyingType = expectedType.GetGenericArguments()[0];
                else
                    underlyingType = expectedType.GetElementType();
                if (obj == null)
                    return null;
                return ConvertObjectToType(obj, underlyingType);
            }
            MethodInfo conMethod = null;
            if (new List<Type>(expectedType.GetInterfaces()).Contains(typeof(IModel)))
            {
                return new InjectableMethod(expectedType.GetMethods(Constants.LOAD_METHOD_FLAGS).FirstOrDefault(mi => mi.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0),log)
                    .Invoke(null,this, pars: new object[] { ((Hashtable)obj)["id"].ToString() });
            }
            foreach (MethodInfo mi in expectedType.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                if (mi.Name == "op_Implicit" || mi.Name == "op_Explicit")
                {
                    if (
                        (
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
            }
            if (conMethod != null)
                return conMethod.Invoke(null, new object[] { obj });
            try
            {
                object ret = Convert.ChangeType(obj, expectedType);
                return ret;
            }
            catch (Exception e)
            {
                log?.LogError("Type conversion error: {}",e.Message);
            }
            return obj;
        }
    }
}
