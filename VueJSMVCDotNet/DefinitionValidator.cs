using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.JSGenerators;
using System;
using System.Collections.Generic;
using System.Reflection;
#if !NETSTANDARD
using System.Runtime.Loader;
#endif
using System.Text;
using System.Text.RegularExpressions;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    internal static class DefinitionValidator
    {
        private struct sPathTypePair
        {
            private string _path;
            public string Path
            {
                get { return _path; }
            }

            private Type _modelType;
            public Type ModelType
            {
                get { return _modelType; }
            }

            public sPathTypePair(string path, Type modelType)
            {
                _path = path;
                _modelType = modelType;
            }
        }

        private static Regex _regListPars = new Regex("\\{(\\d+)\\}", RegexOptions.Compiled | RegexOptions.ECMAScript);

        private static bool _IsValidDataActionMethod(MethodInfo method)
        {
            return (method.ReturnType == typeof(bool)) && (
                method.GetParameters().Length == 0 || 
                (method.GetParameters().Length==1 && Utility.IsISecureSessionType(method.GetParameters()[0].ParameterType))
            );
        }

        /*
         * Called to validate all model definitions through the following checks:
         * 1.  Check to make sure that there is at least 1 route specified for the model.
         * 2.  Check for an empty constructor, and if no empty constructor is specified, ensure that the create method is blocked
         * 3.  Check the all paths specified for the model are unique
         * 4.  Check to make sure only 1 load method exists for a model
         * 5.  Check to make sure that the select model method has the right return type
         * 6.  Check to make sure a Load method exists
         * 7.  Check to make sure that the id property is not blocked.
         * 8.  Check to make sure all paged select lists have proper parameters
         * 9.  Check to make sure all exposed methods are valid (if have same name, have different parameter count)
         * 10.  Check to make sure all exposed slow methods are valid (ensure they have a parameter for the AddItem delegate and their response is void)
         */
#if NETCOREAPP3_1
        internal static List<Exception> Validate(AssemblyLoadContext alc,out List<Type> invalidModels,out List<Type> models)
        {
            Logger.Debug("Attempting to load and validate the models found in the Assembly Load Context {0}", new object[] { alc.Name });
            models = Utility.LocateTypeInstances(typeof(IModel),alc);
            Logger.Debug("Located {0} models in Assembly Load Context {1}", new object[] { models.Count, alc.Name });
#else
        internal static List<Exception> Validate(out List<Type> invalidModels,out List<Type> models)
        {
            models = Utility.LocateTypeInstances(typeof(IModel));
            Logger.Debug("Located {0} models in the system",new object[]{models.Count});
#endif
            List<Exception> errors = new List<Exception>();
            invalidModels = new List<Type>();
            List<sPathTypePair> paths = new List<sPathTypePair>();
            foreach (Type t in models)
            {
                Logger.Debug("Validating Model {0}", new object[] { t.FullName });
                if (t.GetCustomAttributes(typeof(ModelRoute), false).Length == 0)
                {
                    Logger.Trace("Model {0} has no route", new object[] { t.FullName });
                    invalidModels.Add(t);
                    errors.Add(new NoRouteException(t));
                }
                bool hasAdd = false;
                bool hasUpdate = false;
                bool hasDelete = false;
                foreach (MethodInfo mi in t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS))
                {
                    if (mi.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0)
                    {
                        if (hasAdd)
                        {
                            Logger.Trace("Model {0} has more than 1 save method", new object[] { t.FullName });
                            if (!invalidModels.Contains(t))
                                invalidModels.Add(t);
                            errors.Add(new DuplicateModelSaveMethodException(t, mi));
                        }
                        else
                        {
                            hasAdd = true;
                            if (!_IsValidDataActionMethod(mi))
                            {
                                Logger.Trace("Model {0} has and invalid save method", new object[] { t.FullName });
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidModelSaveMethodException(t, mi));
                            }
                        }
                    }
                    else if (mi.GetCustomAttributes(typeof(ModelDeleteMethod), false).Length > 0)
                    {
                        if (hasDelete)
                        {
                            Logger.Trace("Model {0} has more than 1 delete method", new object[] { t.FullName });
                            if (!invalidModels.Contains(t))
                                invalidModels.Add(t);
                            errors.Add(new DuplicateModelDeleteMethodException(t, mi));
                        }
                        else
                        {
                            hasDelete = true;
                            if (!_IsValidDataActionMethod(mi))
                            {
                                Logger.Trace("Model {0} has and invalid delete method", new object[] { t.FullName });
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidModelDeleteMethodException(t, mi));
                            }
                        }
                    }
                    else if (mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0)
                    {
                        if (hasUpdate)
                        {
                            Logger.Trace("Model {0} has more than 1 update method", new object[] { t.FullName });
                            if (!invalidModels.Contains(t))
                                invalidModels.Add(t);
                            errors.Add(new DuplicateModelUpdateMethodException(t, mi));
                        }
                        else
                        {
                            hasUpdate = true;
                            if (!_IsValidDataActionMethod(mi)) { 
                                Logger.Trace("Model {0} has and invalid update method", new object[] { t.FullName });
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidModelUpdateMethodException(t, mi));
                            }
                        }
                    }
                }
                if (hasAdd)
                {
                    if (t.GetConstructor(Type.EmptyTypes) == null)
                    {
                        Logger.Trace("Model {0} has a save method without an empty constructor", new object[] { t.FullName });
                        invalidModels.Add(t);
                        errors.Add(new NoEmptyConstructorException(t));
                    }
                }
                foreach (ModelRoute mr in t.GetCustomAttributes(typeof(ModelRoute), false))
                {
                    Regex reg = new Regex("^(" + (mr.Host == "*" ? ".+" : mr.Host) + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path) + ")$", RegexOptions.ECMAScript | RegexOptions.Compiled);
                    foreach (sPathTypePair p in paths)
                    {
                        if (reg.IsMatch(p.Path) && (p.ModelType.FullName != t.FullName))
                        {
                            Logger.Trace("Model {0} has a model route that is a duplicate of another model", new object[] { t.FullName });
                            if (!invalidModels.Contains(t))
                                invalidModels.Add(t);
                            errors.Add(new DuplicateRouteException(p.Path, p.ModelType, mr.Host + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path), t));
                        }
                    }
                    paths.Add(new sPathTypePair(mr.Host + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path), t));
                }
                bool found = false;
                bool foundLoadAll=false;
                foreach (MethodInfo mi in t.GetMethods(Constants.LOAD_METHOD_FLAGS))
                {
                    if (mi.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0)
                    {
                        if (mi.ReturnType != t)
                        {
                            if (!mi.ReturnType.IsAssignableFrom(t))
                            {
                                Logger.Trace("Model {0} does not return a valid type for its Load method", new object[] { t.FullName });
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidLoadMethodReturnType(t, mi.Name));
                            }
                        }
                        if (mi.ReturnType == t)
                        {
                            if (mi.GetParameters().Length == 1)
                            {
                                if (mi.GetParameters()[0].ParameterType == typeof(string))
                                {
                                    if (found)
                                    {
                                        Logger.Trace("Model {0} has a duplicated load method", new object[] { t.FullName });
                                        if (!invalidModels.Contains(t))
                                            invalidModels.Add(t);
                                        errors.Add(new DuplicateLoadMethodException(t, mi.Name));
                                    }
                                    found = true;
                                }
                            }else if (mi.GetParameters().Length==2){
                                if ((
                                    mi.GetParameters()[0].ParameterType==typeof(string)
                                    && Utility.IsISecureSessionType(mi.GetParameters()[1].ParameterType)
                                )||(
                                    mi.GetParameters()[1].ParameterType==typeof(string)
                                    && Utility.IsISecureSessionType(mi.GetParameters()[0].ParameterType)
                                )){
                                    if (found)
                                    {
                                        Logger.Trace("Model {0} has a duplicated load method", new object[] { t.FullName });
                                        if (!invalidModels.Contains(t))
                                            invalidModels.Add(t);
                                        errors.Add(new DuplicateLoadMethodException(t, mi.Name));
                                    }
                                    found = true;
                                }
                            }
                            else
                            {
                                Logger.Trace("Model {0} has an invalid load method", new object[] { t.FullName });
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidLoadMethodArguements(t, mi.Name));
                            }
                        }
                    }
                    if (mi.GetCustomAttributes(typeof(ModelLoadAllMethod),false).Length>0){
                        Type rtype = mi.ReturnType;
                        if (rtype.IsArray){
                            rtype=rtype.GetElementType();
                        }else if (rtype.IsGenericType && rtype.GetGenericTypeDefinition() == typeof(List<>)){
                            rtype = rtype.GetGenericArguments()[0];
                        }else{
                            rtype=null;
                            Logger.Trace("Model {0} has an invalid return type for ModelLoadAllMethod", new object[] { t.FullName });
                            if (!invalidModels.Contains(t))
                                invalidModels.Add(t);
                            errors.Add(new InvalidLoadAllMethodReturnType(t, mi.Name));
                        }
                        if (rtype!=null){
                            if (rtype!=t){
                                Logger.Trace("Model {0} has an invalid return type for ModelLoadAllMethod", new object[] { t.FullName });
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidLoadAllMethodReturnType(t, mi.Name));
                            }else{
                                if (mi.GetParameters().Length!=0){
                                    if (mi.GetParameters().Length==1){
                                        if (!Utility.IsISecureSessionType(mi.GetParameters()[0].ParameterType)){
                                            Logger.Trace("Model {0} has an invalid arguement for ModelLoadAllMethod", new object[] { t.FullName });
                                            if (!invalidModels.Contains(t))
                                                invalidModels.Add(t);
                                            errors.Add(new InvalidLoadAllArguements(t, mi.Name));
                                        }
                                    }else{
                                        if (!invalidModels.Contains(t))
                                            invalidModels.Add(t);
                                        errors.Add(new InvalidLoadAllArguements(t, mi.Name));
                                    }
                                }else
                                {
                                    if (foundLoadAll){
                                        Logger.Trace("Model {0} has more than 1 ModelLoadAllMethod", new object[] { t.FullName });
                                        if (!invalidModels.Contains(t))
                                            invalidModels.Add(t);
                                        errors.Add(new DuplicateLoadAllMethodException(t, mi.Name));
                                    }
                                    foundLoadAll=true;
                                }
                            }
                        }
                    }
                    if (mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0)
                    {
                        Type rtype = mi.ReturnType;
                        if (rtype.FullName.StartsWith("System.Nullable"))
                        {
                            if (rtype.IsGenericType)
                                rtype = rtype.GetGenericArguments()[0];
                            else
                                rtype = rtype.GetElementType();
                        }
                        if (rtype.IsArray)
                            rtype = rtype.GetElementType();
                        else if (rtype.IsGenericType)
                        {
                            if (rtype.GetGenericTypeDefinition() == typeof(List<>))
                                rtype = rtype.GetGenericArguments()[0];
                        }
                        if (rtype != t)
                        {
                            Logger.Trace("Model {0} has an invalid return type for the model list method {1}", new object[] { t.FullName,mi.Name });
                            if (!invalidModels.Contains(t))
                                invalidModels.Add(t);
                            errors.Add(new InvalidModelListMethodReturnException(t, mi));
                        }
                        bool isPaged = false;
                        foreach (ModelListMethod mlm in mi.GetCustomAttributes(typeof(ModelListMethod), false))
                        {
                            if (mlm.Paged)
                            {
                                isPaged = true;
                                break;
                            }
                        }
                        foreach (ModelListMethod mlm in mi.GetCustomAttributes(typeof(ModelListMethod), false))
                        {
                            MatchCollection mc = _regListPars.Matches(mlm.Path);
                            if (isPaged && !mlm.Paged)
                            {
                                Logger.Trace("Model {0} has a model list method using paging without indicating it {1}", new object[] { t.FullName, mi.Name });
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidModelListNotAllPagedException(t, mi, mlm.Path));
                            }
                            if (mc.Count != Utility.ExtractStrippedParameters(mi).Length - (isPaged ? 3 : 0))
                            {
                                Logger.Trace("Model {0} has missing parameters from the url for the list method {1}", new object[] { t.FullName, mi.Name });
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidModelListParameterCountException(t, mi, mlm.Path));
                            }
                        }
                        ParameterInfo[] pars = Utility.ExtractStrippedParameters(mi);
                        for (int x = 0; x < pars.Length; x++)
                        {
                            ParameterInfo pi = pars[x];
                            if (pi.ParameterType.IsGenericType)
                            {
                                if (pi.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    if (Utility.IsArrayType(pi.ParameterType.GetGenericArguments()[0]))
                                    {
                                        Logger.Trace("Model {0} has an invalid parameter {2} list method {1}", new object[] { t.FullName, mi.Name,pi.Name });
                                        if (!invalidModels.Contains(t))
                                            invalidModels.Add(t);
                                        errors.Add(new InvalidModelListParameterTypeException(t, mi, pi));
                                    }
                                }
                                else
                                {
                                    Logger.Trace("Model {0} has an invalid parameter {2} list method {1}", new object[] { t.FullName, mi.Name, pi.Name });
                                    if (!invalidModels.Contains(t))
                                        invalidModels.Add(t);
                                    errors.Add(new InvalidModelListParameterTypeException(t, mi, pi));
                                }
                            }
                            else if (pi.ParameterType.IsArray)
                            {
                                Logger.Trace("Model {0} has an invalid parameter {2} list method {1}", new object[] { t.FullName, mi.Name, pi.Name });
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidModelListParameterTypeException(t, mi, pi));
                            }
                            if (pi.IsOut && (!isPaged || x != pars.Length - 1))
                            {
                                Logger.Trace("Model {0} has an invalid parameter {2} list method {1}", new object[] { t.FullName, mi.Name, pi.Name });
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidModelListParameterOutException(t, mi, pi));
                            }
                            if (isPaged && x >= pars.Length - 3)
                            {
                                Type ptype = pi.ParameterType;
                                if (pi.IsOut)
                                    ptype = ptype.GetElementType();
                                if (ptype != typeof(int)
                                    && ptype != typeof(long)
                                    && ptype != typeof(short)
                                    && ptype != typeof(uint)
                                    && ptype != typeof(ulong)
                                    && ptype != typeof(ushort))
                                {
                                    Logger.Trace("Model {0} has an invalid parameter {2} list method {1}", new object[] { t.FullName, mi.Name, pi.Name });
                                    if (!invalidModels.Contains(t))
                                        invalidModels.Add(t);
                                    errors.Add(new InvalidModelListPageParameterTypeException(t, mi, pi));
                                }
                            }
                            if (isPaged && x == pars.Length - 1)
                            {
                                if (!pi.IsOut)
                                {
                                    Logger.Trace("Model {0} is not a valid page total parameter {2} list method {1}", new object[] { t.FullName, mi.Name, pi.Name });
                                    if (!invalidModels.Contains(t))
                                        invalidModels.Add(t);
                                    errors.Add(new InvalidModelListPageTotalPagesNotOutException(t, mi, pi));
                                }
                            }
                        }
                    }
                }
                foreach (PropertyInfo pi in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (pi.GetCustomAttributes(typeof(ModelIgnoreProperty), false).Length == 0)
                    {
                        Type rtype = pi.PropertyType;
                        if (rtype.FullName.StartsWith("System.Nullable"))
                        {
                            if (rtype.IsGenericType)
                                rtype = rtype.GetGenericArguments()[0];
                            else
                                rtype = rtype.GetElementType();
                        }
                        if (rtype.IsArray)
                            rtype = rtype.GetElementType();
                        else if (rtype.IsGenericType)
                        {
                            if (rtype.GetGenericTypeDefinition() == typeof(List<>))
                                rtype = rtype.GetGenericArguments()[0];
                        }
                    }
                }
                if (t.GetProperty("id").GetCustomAttributes(typeof(ModelIgnoreProperty), false).Length > 0)
                {
                    Logger.Trace("Model {0} is not valid because the id property is blocked by ModelIgnoreProperty", new object[] { t.FullName });
                    if (!invalidModels.Contains(t))
                        invalidModels.Add(t);
                    errors.Add(new ModelIDBlockedException(t));
                }
                if (!found)
                {
                    Logger.Trace("Model {0} is not valid because no load method was found", new object[] { t.FullName });
                    if (!invalidModels.Contains(t))
                        invalidModels.Add(t);
                    errors.Add(new NoLoadMethodException(t));
                }
                foreach (BindingFlags bf in new BindingFlags[] { Constants.STATIC_INSTANCE_METHOD_FLAGS,Constants.INSTANCE_METHOD_FLAGS })
                {
                    List<string> methods = new List<string>();
                    MethodInfo[] methodInfos = t.GetMethods(bf);
                    foreach (MethodInfo mi in methodInfos)
                    {
                        if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                        {
                            int parCount = 0;
                            bool hasAddItem = false;
                            foreach (ParameterInfo pi in mi.GetParameters())
                            {
                                parCount+=(pi.ParameterType.FullName==typeof(AddItem).FullName ? 0 : 1);
                                hasAddItem|=pi.ParameterType.FullName==typeof(AddItem).FullName;
                            }
                            if (methods.Contains(mi.Name + "." + parCount.ToString()))
                            {
                                Logger.Trace("Model {0} is not valid because the method {1} has a duplicate method signature", new object[] { t.FullName, mi.Name });
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new DuplicateMethodSignatureException(t, mi));
                            }
                            else
                            {
                                bool isValidCall = true;
                                ExposedMethod em = (ExposedMethod)mi.GetCustomAttributes(typeof(ExposedMethod), false)[0];
                                if (hasAddItem)
                                {
                                    if (!em.IsSlow)
                                    {
                                        Logger.Trace("Model {0} is not valid because the method {1} is using the AddItem delegate but is not marked slow", new object[] { t.FullName, mi.Name });
                                        if (!invalidModels.Contains(t))
                                            invalidModels.Add(t);
                                        errors.Add(new MethodNotMarkedAsSlow(t, mi));
                                        isValidCall = false;
                                    }else if (mi.ReturnType!=typeof(void))
                                    {
                                        Logger.Trace("Model {0} is not valid because the method {1} is using the AddItem delegate requires a void response", new object[] { t.FullName, mi.Name });
                                        if (!invalidModels.Contains(t))
                                            invalidModels.Add(t);
                                        errors.Add(new DuplicateMethodSignatureException(t, mi));
                                        isValidCall = false;
                                    }
                                }
                                if (isValidCall)
                                    methods.Add(mi.Name + "." + parCount.ToString());
                            }
                        }
                    }
                }
            }
            return errors;
        }
    }
}
