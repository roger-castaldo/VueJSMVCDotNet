﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System.Collections;
using System.Reflection;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model
{
    internal class ModelRequestData : IRequestData
    {
        private readonly Dictionary<string, object> _formData;
        public IEnumerable<string> Keys => _formData.Keys;

        public T GetValue<T>(string key)
        {
            key = Keys.FirstOrDefault(k => String.Equals(k, key, StringComparison.InvariantCultureIgnoreCase));
            if (key==null)
                throw new KeyNotFoundException();
            var obj = _formData[key];
            try
            {
                if (obj is JsonDocument document)
                    return Utility.JsonDecode<T>(document, _session);
                else if (obj is JsonNode node)
                    return Utility.JsonDecode<T>(node, _session);
                else if (obj is JsonElement element)
                    return Utility.JsonDecode<T>(element, _session);
                else
                    return (T)_ConvertObjectToType(obj, typeof(T), _session);
            }
            catch (Exception)
            {
                throw new InvalidCastException();
            }
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

        private readonly ISecureSession _session;
        public ISecureSession Session => _session;

        public ModelRequestData(Dictionary<string, object> formData, ISecureSession session)
        {
            _formData = formData;
            _session = session;
        }

        private static object _ConvertObjectToType(object obj, Type expectedType, ISecureSession session)
        {
            Logger.Trace("Attempting to convert object of type {0} to {1}", new object[] { (obj == null ? "NULL" : obj.GetType().FullName), expectedType.FullName });
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
            if (expectedType.IsArray || (obj is ArrayList))
            {
                int count = 1;
                Type underlyingType;
                if (expectedType.IsGenericType)
                    underlyingType = expectedType.GetGenericArguments()[0];
                else
                    underlyingType = expectedType.GetElementType();
                if (obj is ArrayList list)
                    count = list.Count;
                Array ret = Array.CreateInstance(underlyingType, count);
                if (!(obj is ArrayList list1))
                    ret.SetValue(_ConvertObjectToType(obj, underlyingType, session), 0);
                else
                {
                    for (int x = 0; x < ret.Length; x++)
                        ret.SetValue(_ConvertObjectToType(list1[x], underlyingType, session), x);
                }
                if (expectedType.FullName.StartsWith("System.Collections.Generic.List"))
                    return expectedType.GetConstructor(new Type[] { ret.GetType() }).Invoke(new object[] { ret });
                return ret;
            }
            if (expectedType.FullName.StartsWith("System.Collections.Generic.Dictionary"))
            {
                object ret = expectedType.GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
                Type keyType = expectedType.GetGenericArguments()[0];
                Type valType = expectedType.GetGenericArguments()[1];
                foreach (string str in ((Hashtable)obj).Keys)
                {
                    ((IDictionary)ret).Add(_ConvertObjectToType(str, keyType, session), _ConvertObjectToType(((Hashtable)obj)[str], valType, session));
                }
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
                return _ConvertObjectToType(obj, underlyingType, session);
            }
            MethodInfo conMethod = null;
            if (new List<Type>(expectedType.GetInterfaces()).Contains(typeof(IModel)))
            {
                return new InjectableMethod(expectedType.GetMethods(Constants.LOAD_METHOD_FLAGS).FirstOrDefault(mi => mi.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0))
                    .Invoke(null, pars: new object[] { ((Hashtable)obj)["id"].ToString() }, session: session);
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
                Logger.LogError(e);
            }
            return obj;
        }
    }
}
