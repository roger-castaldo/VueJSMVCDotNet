using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    /*
     * This class houses some basic utility functions used by other classes to access embedded resources, search
     * for types, etc.
     */
    internal static class Utility
    {
        //houses a cache of Types found through locate type, this is used to increase performance
        private static Dictionary<string, Type> _TYPE_CACHE;
        //houses a cache of Type instances through locate type instances, this is used to increate preformance
        private static Dictionary<string, List<Type>> _INSTANCES_CACHE;

        static Utility()
        {
            _TYPE_CACHE = new Dictionary<string, Type>();
            _INSTANCES_CACHE = new Dictionary<string, List<Type>>();
        }

        //Called to locate a type by its name, this scans through all assemblies 
        //which by default Type.Load does not perform.
        public static Type LocateType(string typeName)
        {
            Logger.Debug("Attempting to locate type " + typeName);
            Type t = null;
            lock (_TYPE_CACHE)
            {
                if (_TYPE_CACHE.ContainsKey(typeName))
                    t = _TYPE_CACHE[typeName];
            }
            if (t == null)
            {
                t = Type.GetType(typeName, false, true);
                if (t == null)
                {
                    foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            if (ass.GetName().Name != "mscorlib" && !ass.GetName().Name.StartsWith("System.") && ass.GetName().Name != "System" && !ass.GetName().Name.StartsWith("Microsoft"))
                            {
                                t = ass.GetType(typeName, false, true);
                                if (t != null)
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            if (e.Message != "The invoked member is not supported in a dynamic assembly.")
                            {
                                throw e;
                            }
                        }
                    }
                }
                lock (_TYPE_CACHE)
                {
                    if (!_TYPE_CACHE.ContainsKey(typeName))
                        _TYPE_CACHE.Add(typeName, t);
                }
            }
            return t;
        }

        internal static void SetModelValues(Hashtable data, ref IModel model, bool isNew)
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
                            if (pi.GetCustomAttributes(typeof(ReadOnlyModelProperty),true).Length==0 || isNew)
                            {
                                Type propType = pi.PropertyType;
                                if (propType.IsArray)
                                    propType = propType.GetElementType();
                                else if (propType.IsGenericType)
                                {
                                    if (propType.GetGenericTypeDefinition() == typeof(List<>))
                                        propType = propType.GetGenericArguments()[0];
                                }
                                var obj = _ConvertObjectToType(data[str], pi.PropertyType);
                                pi.SetValue(model, obj);
                            }
                        }
                    }
                }
            }
        }

        internal static void LocateMethod(Hashtable formData,List<MethodInfo> methods,ISecureSession session,out MethodInfo method,out object[] pars)
        {
            method = null;
            pars = null;
            int idx=-1;
            if (formData == null || formData.Count == 0)
            {
                foreach (MethodInfo mi in methods)
                {
                    if (mi.GetParameters().Length == 0
                        || UsesSecureSession(mi,out idx))
                    {
                        method = mi;
                        pars = (idx==-1 ? new object[] { } : new object[]{session});
                        return;
                    }
                }
            }
            else
            {
                foreach (MethodInfo m in methods)
                {
                    bool useSession = UsesSecureSession(m,out idx);
                    if (m.GetParameters().Length == formData.Count
                    || (useSession && m.GetParameters().Length==formData.Count+1))
                    {
                        pars = new object[formData.Count+(useSession ? 1 : 0)];
                        bool isMethod = true;
                        int index = 0;
                        foreach (ParameterInfo pi in m.GetParameters())
                        {
                            if (index==idx){
                                pars[idx]=session;
                                index++;
                            }else{
                                if (formData.ContainsKey(pi.Name))
                                    pars[index] = _ConvertObjectToType(formData[pi.Name], pi.ParameterType);
                                else
                                {
                                    isMethod = false;
                                    break;
                                }
                                index++;
                            }
                        }
                        if (isMethod)
                        {
                            method = m;
                            return;
                        }
                    }
                }
            }
        }

        /*
         * Called to convert a given json object to the expected type.
         */
        private static object _ConvertObjectToType(object obj, Type expectedType)
        {
            Logger.Debug("Attempting to convert object of type " + (obj == null ? "NULL" : obj.GetType().FullName) + " to " + expectedType.FullName);
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
                Type underlyingType = null;
                if (expectedType.IsGenericType)
                    underlyingType = expectedType.GetGenericArguments()[0];
                else
                    underlyingType = expectedType.GetElementType();
                if (obj is ArrayList)
                    count = ((ArrayList)obj).Count;
                Array ret = Array.CreateInstance(underlyingType, count);
                if (!(obj is ArrayList))
                {
                    ret.SetValue(_ConvertObjectToType(obj, underlyingType), 0);
                }
                else
                {
                    for (int x = 0; x < ret.Length; x++)
                        ret.SetValue(_ConvertObjectToType(((ArrayList)obj)[x], underlyingType), x);
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
                    ((IDictionary)ret).Add(_ConvertObjectToType(str, keyType), _ConvertObjectToType(((Hashtable)obj)[str], valType));
                }
                return ret;
            }
            if (expectedType.FullName.StartsWith("System.Nullable"))
            {
                Type underlyingType = null;
                if (expectedType.IsGenericType)
                    underlyingType = expectedType.GetGenericArguments()[0];
                else
                    underlyingType = expectedType.GetElementType();
                if (obj == null)
                    return null;
                return _ConvertObjectToType(obj, underlyingType);
            }
            if (new List<Type>(expectedType.GetInterfaces()).Contains(typeof(IModel)))
            {
                object ret = null;
                MethodInfo loadMethod = null;
                foreach (MethodInfo mi in expectedType.GetMethods(Constants.LOAD_METHOD_FLAGS))
                {
                    if (mi.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0)
                    {
                        loadMethod = mi;
                        break;
                    }
                }
                if (loadMethod == null)
                {
                    MethodInfo conMethod = null;
                    foreach (MethodInfo mi in expectedType.GetMethods(BindingFlags.Static | BindingFlags.Public))
                    {
                        if (mi.Name == "op_Implicit" || mi.Name == "op_Explicit")
                        {
                            if (mi.ReturnType.Equals(expectedType)
                                && mi.GetParameters().Length == 1
                                && mi.GetParameters()[0].ParameterType.Equals(obj.GetType()))
                            {
                                conMethod = mi;
                                break;
                            }
                        }
                    }
                    if (conMethod != null)
                        ret = conMethod.Invoke(null, new object[] { obj });
                    else
                    {
                        ret = expectedType.GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
                        foreach (string str in ((Hashtable)obj).Keys)
                        {
                            PropertyInfo pi = expectedType.GetProperty(str);
                            pi.SetValue(ret, _ConvertObjectToType(((Hashtable)obj)[str], pi.PropertyType), new object[0]);
                        }
                    }
                }
                else
                    ret = loadMethod.Invoke(null, new object[] { ((Hashtable)obj)["id"] });
                return ret;
            }
            try
            {
                object ret = Convert.ChangeType(obj, expectedType);
                return ret;
            }
            catch (Exception e)
            {
            }
            return obj;
        }

        //Called to locate all child classes of a given parent type
        public static List<Type> LocateTypeInstances(Type parent)
        {
            Logger.Debug("Attempting to locate instances of type " + parent.FullName);
            List<Type> ret = null;
            lock (_INSTANCES_CACHE)
            {
                if (_INSTANCES_CACHE.ContainsKey(parent.FullName))
                    ret = _INSTANCES_CACHE[parent.FullName];
            }
            if (ret == null)
            {
                ret = new List<Type>();
                foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (ass.GetName().Name != "mscorlib" && !ass.GetName().Name.StartsWith("System.") && ass.GetName().Name != "System" && !ass.GetName().Name.StartsWith("Microsoft"))
                    {
                        foreach (Type t in _GetLoadableTypes(ass))
                        {
                            if (t.IsSubclassOf(parent) || (parent.IsInterface && new List<Type>(t.GetInterfaces()).Contains(parent)))
                                ret.Add(t);
                        }
                    }
                }
                lock (_INSTANCES_CACHE)
                {
                    if (!_INSTANCES_CACHE.ContainsKey(parent.FullName))
                        _INSTANCES_CACHE.Add(parent.FullName, ret);
                }
            }
            return ret;
        }

        private static Type[] _GetLoadableTypes(Assembly ass)
        {
            Type[] ret;
            try
            {
                ret = ass.GetTypes();
            }
            catch (ReflectionTypeLoadException rtle)
            {
                ret = rtle.Types;
            }
            catch (Exception e)
            {
                if (e.Message != "The invoked member is not supported in a dynamic assembly."
                            && !e.Message.StartsWith("Unable to load one or more of the requested types."))
                    throw e;
                else
                    ret = new Type[0];
            }
            return ret;
        }

        internal static void ClearCaches()
        {
            lock (_INSTANCES_CACHE)
            {
                _INSTANCES_CACHE.Clear();
            }
            lock (_TYPE_CACHE)
            {
                _TYPE_CACHE.Clear();
            }
        }

        internal static List<PropertyInfo> GetModelProperties(Type modelType)
        {
            List<PropertyInfo> props = new List<PropertyInfo>();
            foreach (PropertyInfo pi in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (pi.GetCustomAttributes(typeof(ModelIgnoreProperty), false).Length == 0 && pi.Name != "id")
                {
                    if (!pi.PropertyType.FullName.Contains("+KeyCollection") && pi.GetGetMethod().GetParameters().Length == 0)
                        props.Add(pi);
                }
            }
            return props;
        }

        internal static string GetModelUrlRoot(Type modelType)
        {
            string urlRoot = "";
            foreach (ModelRoute mr in modelType.GetCustomAttributes(typeof(ModelRoute), false))
            {
                urlRoot = mr.Path;
                break;
            }
            return urlRoot;
        }

        private static Regex _regNoCache = new Regex("[?&]_=(\\d+)$", RegexOptions.Compiled | RegexOptions.ECMAScript);

        public static string CleanURL(Uri url)
        {
            return _regNoCache.Replace(url.PathAndQuery, "");
        }

        public static Uri BuildURL(HttpContext context)
        {
            UriBuilder builder = new UriBuilder(
                context.Request.Scheme,
                context.Request.Host.Host,
                (context.Request.Host.Port.HasValue ? context.Request.Host.Port.Value : (context.Request.IsHttps ? 443 : 80)),
                context.Request.Path
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

        public static IModel InvokeLoad(MethodInfo mi,string id,ISecureSession session){
            List<object> pars = new List<object>();
            ParameterInfo[] mpars = mi.GetParameters();
            if (mpars.Length==1)
                pars.Add(id);
            else{
                if (mpars[0].ParameterType == typeof(string)){
                    pars.AddRange(new object[]{id,session});
                }else{
                    pars.AddRange(new object[]{session,id});
                }
            }
            return (IModel)mi.Invoke(null,pars.ToArray());
        }

        public static bool UsesSecureSession(MethodInfo mi,out int index){
            index=-1;
            ParameterInfo[] pars = mi.GetParameters();
            for(int x=0;x<pars.Length;x++){
                if (IsISecureSessionType(pars[x].ParameterType)) {
                    index=x;
                    return true;
                }
            }
            return false;
        }

        public static bool IsISecureSessionType(Type type)
        {
            return type == typeof(ISecureSession) ||
                new List<Type>(type.GetInterfaces()).Contains(typeof(ISecureSession));
        }

        public static ParameterInfo[] ExtractStrippedParameters(MethodInfo mi){
            int idx;
            List<ParameterInfo> ret = new List<ParameterInfo>(mi.GetParameters());
            if (UsesSecureSession(mi,out idx)){
                ret.RemoveAt(idx);
            }
            return ret.ToArray();
        }
    }
}
