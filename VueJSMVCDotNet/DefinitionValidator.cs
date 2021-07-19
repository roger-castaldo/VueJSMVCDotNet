using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.JSGenerators;
using System;
using System.Collections.Generic;
using System.Reflection;
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
         * 6.  Check to make sure that the select model method has the right return type
         * 8.  Check to make sure a Load method exists
         * 9.  Check to make sure that the id property is not blocked.
         * 12.  Check to make sure all paged select lists have proper parameters
         * 13.  Check to make sure all exposed methods are valid (if have same name, have different parameter count)
         */
        internal static List<Exception> Validate(out List<Type> invalidModels,out List<Type> models)
        {
            List<Exception> errors = new List<Exception>();
            invalidModels = new List<Type>();
            List<sPathTypePair> paths = new List<sPathTypePair>();
            models = Utility.LocateTypeInstances(typeof(IModel));
            foreach (Type t in models)
            {
                if (t.GetCustomAttributes(typeof(ModelRoute), false).Length == 0)
                {
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
                            if (!invalidModels.Contains(t))
                                invalidModels.Add(t);
                            errors.Add(new DuplicateModelSaveMethodException(t, mi));
                        }
                        else
                        {
                            hasAdd = true;
                            if (!_IsValidDataActionMethod(mi))
                            {
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
                            if (!invalidModels.Contains(t))
                                invalidModels.Add(t);
                            errors.Add(new DuplicateModelDeleteMethodException(t, mi));
                        }
                        else
                        {
                            hasDelete = true;
                            if (!_IsValidDataActionMethod(mi))
                            {
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
                            if (!invalidModels.Contains(t))
                                invalidModels.Add(t);
                            errors.Add(new DuplicateModelUpdateMethodException(t, mi));
                        }
                        else
                        {
                            hasUpdate = true;
                            if (!_IsValidDataActionMethod(mi))
                            {
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
                        if (mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0)
                        {
                            if (!invalidModels.Contains(t))
                                invalidModels.Add(t);
                            errors.Add(new InvalidModelListMethodReturnException(t, mi));
                        }
                        if (mi.ReturnType != t)
                        {
                            if (!mi.ReturnType.IsAssignableFrom(t))
                            {
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
                                        if (!invalidModels.Contains(t))
                                            invalidModels.Add(t);
                                        errors.Add(new DuplicateLoadMethodException(t, mi.Name));
                                    }
                                    found = true;
                                }
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
                            if (!invalidModels.Contains(t))
                                invalidModels.Add(t);
                            errors.Add(new InvalidLoadAllMethodReturnType(t, mi.Name));
                        }
                        if (rtype!=null){
                            if (rtype!=t){
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidLoadAllMethodReturnType(t, mi.Name));
                            }else{
                                if (mi.GetParameters().Length!=0){
                                    if (mi.GetParameters().Length==1){
                                        if (!Utility.IsISecureSessionType(mi.GetParameters()[0].ParameterType)){
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
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidModelListNotAllPagedException(t, mi, mlm.Path));
                            }
                            if (mc.Count != Utility.ExtractStrippedParameters(mi).Length - (isPaged ? 3 : 0))
                            {
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
                                        if (!invalidModels.Contains(t))
                                            invalidModels.Add(t);
                                        errors.Add(new InvalidModelListParameterTypeException(t, mi, pi));
                                    }
                                }
                                else
                                {
                                    if (!invalidModels.Contains(t))
                                        invalidModels.Add(t);
                                    errors.Add(new InvalidModelListParameterTypeException(t, mi, pi));
                                }
                            }
                            else if (pi.ParameterType.IsArray)
                            {
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidModelListParameterTypeException(t, mi, pi));
                            }
                            if (pi.IsOut && (!isPaged || x != pars.Length - 1))
                            {
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
                                    if (!invalidModels.Contains(t))
                                        invalidModels.Add(t);
                                    errors.Add(new InvalidModelListPageParameterTypeException(t, mi, pi));
                                }
                            }
                            if (isPaged && x == pars.Length - 1)
                            {
                                if (!pi.IsOut)
                                {
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
                    if (!invalidModels.Contains(t))
                        invalidModels.Add(t);
                    errors.Add(new ModelIDBlockedException(t));
                }
                if (!found)
                {
                    if (!invalidModels.Contains(t))
                        invalidModels.Add(t);
                    errors.Add(new NoLoadMethodException(t));
                }
                List<string> methods = new List<string>();
                foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                    {
                        if (methods.Contains(mi.Name + "." + mi.GetParameters().Length.ToString()))
                        {
                            if (!invalidModels.Contains(t))
                                invalidModels.Add(t);
                            errors.Add(new DuplicateMethodSignatureException(t, mi));
                        }
                        else
                            methods.Add(mi.Name + "." + mi.GetParameters().Length.ToString());
                    }
                }
            }
            return errors;
        }
    }
}
