using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
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
        //houses the assembly load contexts for types
        private static Dictionary<string,List<Type>> _LOAD_CONTEXT_TYPE_SOURCES;

        static Utility()
        {
            _TYPE_CACHE = new Dictionary<string, Type>();
            _INSTANCES_CACHE = new Dictionary<string, List<Type>>();
            _LOAD_CONTEXT_TYPE_SOURCES = new Dictionary<string,List<Type>>();
        }

        //Called to locate a type by its name, this scans through all assemblies 
        //which by default Type.Load does not perform.
        public static Type LocateType(string typeName)
        {
            Logger.Trace("Attempting to locate type {0}",new object[] { typeName });
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
                    foreach (AssemblyLoadContext alc in AssemblyLoadContext.All){
                        foreach (Assembly ass in alc.Assemblies)
                        {
                            try
                            {
                                if (ass.GetName().Name != "mscorlib" && !ass.GetName().Name.StartsWith("System.") && ass.GetName().Name != "System" && !ass.GetName().Name.StartsWith("Microsoft"))
                                {
                                    t = ass.GetType(typeName, false, true);
                                    if (t != null){
                                        #if NET
                                        _MarkTypeSource(alc.Name,t);
                                        #endif
                                        break;
                                    }
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
                                Logger.Trace("Attempting to convert the value supplied for property {0}.{1} to {2}", new object[] { model.GetType().FullName, pi.Name, pi.PropertyType });
                                var obj = _ConvertObjectToType(data[str], pi.PropertyType);
                                Logger.Trace("Setting mode property {0}.{1} with converted value", new object[] { model.GetType(), pi.Name });
                                pi.SetValue(model, obj);
                            }
                        }
                    }
                }
            }
        }

        internal static void LocateMethod(Hashtable formData,List<MethodInfo> methods,out MethodInfo method,out object[] pars)
        {
            method = null;
            pars = null;
            if (formData == null || formData.Count == 0)
                method = methods.Where(mi=>ExtractStrippedParameters(mi).Count()==0).FirstOrDefault();
            else
            {
                foreach (MethodInfo m in methods.Where(mi=> ExtractStrippedParameters(mi).Count()==formData.Count))
                {
                    var notNullArguement = (NotNullArguement)m.GetCustomAttribute(typeof(NotNullArguement));
                    pars = new object[formData.Count];
                    bool isMethod = true;
                    int index = 0;
                    foreach (ParameterInfo pi in ExtractStrippedParameters(m))
                    {
                        if (formData.ContainsKey(pi.Name) && (notNullArguement==null||!(!notNullArguement.IsParameterNullable(pi)&&formData[pi.Name]==null)))
                            pars[index] = _ConvertObjectToType(formData[pi.Name], pi.ParameterType);
                        else
                        {
                            isMethod = false;
                            break;
                        }
                        index++;
                    }
                    if (isMethod)
                    {
                        method = m;
                        return;
                    }
                }
            }
        }

        /*
         * Called to convert a given json object to the expected type.
         */
        private static object _ConvertObjectToType(object obj, Type expectedType)
        {
            Logger.Trace("Attempting to convert object of type {0} to {1}",new object[] { (obj == null ? "NULL" : obj.GetType().FullName), expectedType.FullName });
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
                    ret.SetValue(_ConvertObjectToType(obj, underlyingType), 0);
                else
                {
                    for (int x = 0; x < ret.Length; x++)
                        ret.SetValue(_ConvertObjectToType(list1[x], underlyingType), x);
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
                Type underlyingType;
                if (expectedType.IsGenericType)
                    underlyingType = expectedType.GetGenericArguments()[0];
                else
                    underlyingType = expectedType.GetElementType();
                if (obj == null)
                    return null;
                return _ConvertObjectToType(obj, underlyingType);
                //return Activator.CreateInstance(expectedType, _ConvertObjectToType(obj, underlyingType));
            }
            MethodInfo conMethod = null;
            if (new List<Type>(expectedType.GetInterfaces()).Contains(typeof(IModel)))
            {
                MethodInfo loadMethod = null;
                foreach (MethodInfo mi in expectedType.GetMethods(Constants.LOAD_METHOD_FLAGS))
                {
                    if (mi.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0)
                    {
                        loadMethod = mi;
                        break;
                    }
                }
                object ret;
                if (loadMethod == null)
                {
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
                    ret = loadMethod.Invoke(null, new object[] { (((Hashtable)obj)["id"]).ToString() });
                return ret;
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

        //Called to locate all child classes of a given parent type
        public static List<Type> LocateTypeInstances(Type parent)
        {
            Logger.Trace("Attempting to locate instances of type {0}",new object[] { parent.FullName });
            List<Type> ret = null;
            lock (_INSTANCES_CACHE)
            {
                if (_INSTANCES_CACHE.ContainsKey(parent.FullName))
                    ret = _INSTANCES_CACHE[parent.FullName];
            }
            if (ret==null)
            {
                ret = new List<Type>();
                foreach (AssemblyLoadContext acl in AssemblyLoadContext.All)
                    ret.AddRange(LocateTypeInstances(parent, acl));
            }
            lock (_INSTANCES_CACHE)
            {
                if (!_INSTANCES_CACHE.ContainsKey(parent.FullName))
                    _INSTANCES_CACHE.Add(parent.FullName, ret);
            }
            return ret;
        }

        public static List<Type> LocateTypeInstances(Type parent,AssemblyLoadContext alc){
            Logger.Trace("Locating Instance types of {0} in the Load Context {1}", new object[] { parent.FullName, alc.Name });
            List<Type> ret = _LocateTypeInstances(parent,alc.Assemblies);
            foreach (Type t in ret){
                _MarkTypeSource(alc.Name,t);
            }
            return ret;
        }

        private static List<Type> _LocateTypeInstances(Type parent,IEnumerable<Assembly> assemblies)
        {
            List<Type> ret = new List<Type>();
            foreach (Assembly ass in assemblies)
            {
                if (ass.GetName().Name != "mscorlib" && !ass.GetName().Name.StartsWith("System.") && ass.GetName().Name != "System" && !ass.GetName().Name.StartsWith("Microsoft"))
                {
                    foreach (Type t in _GetLoadableTypes(ass))
                    {
                        if (t.IsSubclassOf(parent) || (parent.IsInterface && new List<Type>(t.GetInterfaces()).Contains(parent))){
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
                    throw e;
                else
                    ret = new Type[0];
            }
            return ret;
        }

        private static void _MarkTypeSource(string contextName,Type type){
            Logger.Trace("Marking the Assembly Load Context of {0} for the type {1}", new object[] { contextName, type.FullName });
            lock(_LOAD_CONTEXT_TYPE_SOURCES)
            {
                List<Type> types = new List<Type>();
                if (_LOAD_CONTEXT_TYPE_SOURCES.ContainsKey(contextName)){
                    types = _LOAD_CONTEXT_TYPE_SOURCES[contextName];
                    _LOAD_CONTEXT_TYPE_SOURCES.Remove(contextName);
                }
                if (!types.Contains(type)){
                    types.Add(type);
                }
                _LOAD_CONTEXT_TYPE_SOURCES.Add(contextName,types);
            }
        }

        internal static List<Type> UnloadAssemblyContext(string contextName){
            List<Type> ret = null;
            lock(_LOAD_CONTEXT_TYPE_SOURCES){
                if (_LOAD_CONTEXT_TYPE_SOURCES.ContainsKey(contextName)){
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
            lock(_LOAD_CONTEXT_TYPE_SOURCES){
                _LOAD_CONTEXT_TYPE_SOURCES.Clear();
            }
        }

        internal static string GetModelUrlRoot(Type modelType)
        {
            return GetModelUrlRoot(modelType, null);
        }

        internal static string GetModelUrlRoot(Type modelType,string urlBase)
        {
            string urlRoot = (urlBase??"");
            foreach (ModelRoute mr in modelType.GetCustomAttributes(typeof(ModelRoute), false).Cast<ModelRoute>())
            {
                urlRoot += mr.Path;
                break;
            }
            return urlRoot.Replace("//","/");
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
                (urlBase==null ? context.Request.Path.ToString() : context.Request.Path.ToString().Replace(urlBase,"/"))
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

        public static object InvokeMethod(MethodInfo mi,object obj, object[] pars=null,ISecureSession session=null, AddItem addItem=null)
        {
            ParameterInfo[] parameters = mi.GetParameters();
            object[] mpars = new object[parameters.Length];
            int sidx = -1;
            int aidx = -1;
            int lidx = -1;
            if (UsesSecureSession(mi, out sidx))
                mpars[sidx]=session;
            if (UsesAddItem(mi, out aidx))
                mpars[aidx]=addItem;
            if (UsesLog(mi, out lidx))
                mpars[lidx]=Logger.Instance;
            int index = 0;
            for(int x = 0; x<mpars.Length; x++)
            {
                if (x!=sidx&&x!=aidx&&x!=lidx)
                {
                    mpars[x]=pars[index];
                    index++;
                }
            }
            object ret =  mi.Invoke(obj, mpars);
            if (parameters.Count(p=>p.IsOut)>0)
            {
                index = 0;
                for(int x = 0; x<parameters.Length; x++) {
                    if (x!=sidx&&x!=aidx&&x!=lidx)
                    {
                        if (parameters[x].IsOut)
                            pars[index]=mpars[x];
                        index++;
                    }
                }
            }
            return ret;
        }

        public static IModel InvokeLoad(MethodInfo mi,string id,ISecureSession session){
            return (IModel)InvokeMethod(mi, null, new object[] { id }, session: session);
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

        public static bool UsesAddItem(MethodInfo mi, out int index)
        {
            index=-1;
            ParameterInfo[] pars = mi.GetParameters();
            for (int x = 0; x<pars.Length; x++)
            {
                if (pars[x].ParameterType==typeof(AddItem))
                {
                    index=x;
                    return true;
                }
            }
            return false;
        }

        public static bool UsesLog(MethodInfo mi,out int index)
        {
            index=-1;
            ParameterInfo[] pars = mi.GetParameters();
            for (int x = 0; x<pars.Length; x++)
            {
                if (pars[x].ParameterType==typeof(ILog))
                {
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
            List<ParameterInfo> ret = new List<ParameterInfo>(mi.GetParameters());
            if (UsesSecureSession(mi,out int idx))
                ret.RemoveAt(idx);
            if (UsesAddItem(mi, out idx))
                ret.RemoveAt(idx);
            if (UsesLog(mi, out idx))
                ret.RemoveAt(idx);
            return ret.ToArray();
        }

        internal static string GetTypeString(Type propertyType,bool notNullTagged)
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
                }else if (sub!="" && sub!=".")
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
    }
}
