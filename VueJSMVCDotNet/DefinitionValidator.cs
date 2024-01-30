using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet
{
    internal static class DefinitionValidator
    {
        private readonly struct SPathTypePair
        {
            public string Path { get; private init; }
            public Type ModelType { get; private init; }

            public SPathTypePair(string path, Type modelType)
            {
                Path = path;
                ModelType = modelType;
            }
        }

        private static bool IsValidDataActionMethod(MethodInfo method, ILogger log)
        {
            return (method.ReturnType == typeof(bool)) && new InjectableMethod(method,log).StrippedParameters.Length==0;
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
        internal static List<Exception> Validate(AssemblyLoadContext alc,ILogger log,out List<Type> invalidModels,out List<Type> models)
        {
            log?.LogDebug("Attempting to load and validate the models found in the Assembly Load Context {Name}",alc.Name);
            models = Utility.LocateTypeInstances(typeof(IModel),alc,log);
            log?.LogDebug("Located {Count} models in Assembly Load Context {Name}", models.Count, alc.Name);
            List<Exception> errors = new();
            invalidModels = new();
            List<SPathTypePair> paths = new();
            foreach (Type t in models)
            {
                log?.LogDebug("Validating Model {FullName}",  t.FullName);
                if (t.GetCustomAttributes(typeof(ModelRoute), false).Length == 0)
                {
                    log?.LogTrace("Model {FullName} has no route", t.FullName);
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
                            log?.LogTrace("Model {FullName} has more than 1 save method", t.FullName);
                            invalidModels.Add(t);
                            errors.Add(new DuplicateModelSaveMethodException(t, mi));
                        }
                        else
                        {
                            hasAdd = true;
                            if (!IsValidDataActionMethod(mi, log))
                            {
                                log?.LogTrace("Model {FullNane} has and invalid save method", t.FullName);
                                invalidModels.Add(t);
                                errors.Add(new InvalidModelSaveMethodException(t, mi));
                            }
                        }
                    }
                    else if (mi.GetCustomAttributes(typeof(ModelDeleteMethod), false).Length > 0)
                    {
                        if (hasDelete)
                        {
                            log?.LogTrace("Model {FullName} has more than 1 delete method", t.FullName );
                            invalidModels.Add(t);
                            errors.Add(new DuplicateModelDeleteMethodException(t, mi));
                        }
                        else
                        {
                            hasDelete = true;
                            if (!IsValidDataActionMethod(mi, log))
                            {
                                log?.LogTrace("Model {FullName} has and invalid delete method", t.FullName);
                                invalidModels.Add(t);
                                errors.Add(new InvalidModelDeleteMethodException(t, mi));
                            }
                        }
                    }
                    else if (mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0)
                    {
                        if (hasUpdate)
                        {
                            log?.LogTrace("Model {FullName} has more than 1 update method", t.FullName );
                            invalidModels.Add(t);
                            errors.Add(new DuplicateModelUpdateMethodException(t, mi));
                        }
                        else
                        {
                            hasUpdate = true;
                            if (!IsValidDataActionMethod(mi, log)) { 
                                log?.LogTrace("Model {FullName} has and invalid update method", t.FullName);
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
                        log?.LogTrace("Model {FullName} has a save method without an empty constructor", t.FullName);
                        invalidModels.Add(t);
                        errors.Add(new NoEmptyConstructorException(t));
                    }
                }
                foreach (ModelRoute mr in t.GetCustomAttributes<ModelRoute>(false))
                {
                    Regex reg = new("^(" + (mr.Host == "*" ? ".+" : mr.Host) + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path) + ")$", RegexOptions.ECMAScript | RegexOptions.Compiled);
                    foreach (SPathTypePair p in paths)
                    {
                        if (reg.IsMatch(p.Path) && (p.ModelType.FullName != t.FullName))
                        {
                            log?.LogTrace("Model {FullName} has a model route that is a duplicate of another model", t.FullName);
                            invalidModels.Add(t);
                            errors.Add(new DuplicateRouteException(p.Path, p.ModelType, mr.Host + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path), t));
                        }
                    }
                    paths.Add(new SPathTypePair(mr.Host + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path), t));
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
                                log?.LogTrace("Model {FullName} does not return a valid type for its Load method", t.FullName );
                                invalidModels.Add(t);
                                errors.Add(new InvalidLoadMethodReturnType(t, mi.Name));
                            }
                        }
                        if (mi.ReturnType == t)
                        {
                            ParameterInfo[] pars = new InjectableMethod(mi,log).StrippedParameters;
                            if (pars.Length==1 && pars[0].ParameterType==typeof(string))
                            {
                                if (found)
                                {
                                    log?.LogTrace("Model {FullName} has a duplicated load method", t.FullName);
                                    invalidModels.Add(t);
                                    errors.Add(new DuplicateLoadMethodException(t, mi.Name));
                                }
                                found = true;
                            }else{
                                log?.LogTrace("Model {FullName} has an invalid load method", t.FullName);
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
                            log?.LogTrace("Model {FullName} has an invalid return type for ModelLoadAllMethod", t.FullName);
                            invalidModels.Add(t);
                            errors.Add(new InvalidLoadAllMethodReturnType(t, mi.Name));
                        }
                        if (rtype!=null){
                            if (rtype!=t){
                                log?.LogTrace("Model {FullName} has an invalid return type for ModelLoadAllMethod", t.FullName);
                                invalidModels.Add(t);
                                errors.Add(new InvalidLoadAllMethodReturnType(t, mi.Name));
                            }else{
                                ParameterInfo[] pars = new InjectableMethod(mi, log).StrippedParameters;
                                if (pars.Length!=0){
                                    invalidModels.Add(t);
                                    errors.Add(new InvalidLoadAllArguements(t, mi.Name));
                                }else{
                                    if (foundLoadAll){
                                        log?.LogTrace("Model {FullName} has more than 1 ModelLoadAllMethod", t.FullName);
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
                        ModelListMethod mlm = (ModelListMethod)mi.GetCustomAttributes(typeof(ModelListMethod), false)[0];
                        Type rtype = mi.ReturnType;
                        if (rtype.FullName.StartsWith("System.Nullable"))
                            rtype = rtype.GetGenericArguments()[0];
                        if (rtype.IsArray)
                            rtype = rtype.GetElementType();
                        else if (rtype.IsGenericType && rtype.GetGenericTypeDefinition().GetInterfaces().Any(t=>t==typeof(System.Collections.IEnumerable)))
                                rtype = rtype.GetGenericArguments()[0];
                        if (rtype != t)
                        {
                            log?.LogTrace("Model {FullName} has an invalid return type for the model list method {Name}", t.FullName,mi.Name);
                            invalidModels.Add(t);
                            errors.Add(new InvalidModelListMethodReturnException(t, mi));
                        }
                        ParameterInfo[] pars = new InjectableMethod(mi, log).StrippedParameters;
                        if (mlm.Paged && pars.Length<3)
                        {
                            log?.LogTrace("Model {FullName} has an invalid signature for paged model list method {Name}, required parameters are missing",  t.FullName, mi.Name);
                            invalidModels.Add(t);
                            errors.Add(new InvalidModelListParameterCountException(t, mi));
                        }
                        for (int x = 0; x < pars.Length; x++)
                        {
                            ParameterInfo pi = pars[x];
                            if (pi.IsOut && (!mlm.Paged || x != pars.Length - 1))
                            {
                                log?.LogTrace("Model {} list method {} with the parameter {}",  t.FullName, mi.Name, pi.Name);
                                invalidModels.Add(t);
                                errors.Add(new InvalidModelListParameterOutException(t, mi, pi));
                            }
                            if (mlm.Paged && x >= pars.Length - 3)
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
                                    log?.LogTrace("Model {} has an invalid parameter {} list method {}",  t.FullName,pi.Name, mi.Name);
                                    invalidModels.Add(t);
                                    errors.Add(new InvalidModelListPageParameterTypeException(t, mi, pi));
                                }
                            }
                            if (mlm.Paged && x == pars.Length - 1 && !pi.IsOut)
                            {
                                log?.LogTrace("Model {} is not a valid page total parameter {} list method {}", t.FullName,pi.Name, mi.Name);
                                invalidModels.Add(t);
                                errors.Add(new InvalidModelListPageTotalPagesNotOutException(t, mi, pi));
                            }
                        }
                    }
                }
                if (t.GetProperty("id").GetCustomAttributes(typeof(ModelIgnoreProperty), false).Length > 0)
                {
                    log?.LogTrace("Model {} is not valid because the id property is blocked by ModelIgnoreProperty", t.FullName);
                    invalidModels.Add(t);
                    errors.Add(new ModelIDBlockedException(t));
                }
                if (!found)
                {
                    log?.LogTrace("Model {} is not valid because no load method was found", t.FullName );
                    invalidModels.Add(t);
                    errors.Add(new NoLoadMethodException(t));
                }
                foreach (BindingFlags bf in new BindingFlags[] { Constants.STATIC_INSTANCE_METHOD_FLAGS,Constants.INSTANCE_METHOD_FLAGS })
                {
                    List<string> methods = new();
                    MethodInfo[] methodInfos = t.GetMethods(bf);
                    foreach (MethodInfo mi in methodInfos)
                    {
                        if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                        {
                            var im = new InjectableMethod(mi, log);
                            bool hasAddItem = im.HasAddItem;
                            int parCount = im.StrippedParameters.Length;
                            if (methods.Contains(mi.Name + "." + parCount.ToString()))
                            {
                                log?.LogTrace("Model {} is not valid because the method {} has a duplicate method signature", t.FullName, mi.Name);
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
                                        log?.LogTrace("Model {} is not valid because the method {} is using the AddItem delegate but is not marked slow", t.FullName, mi.Name);
                                        invalidModels.Add(t);
                                        errors.Add(new MethodNotMarkedAsSlow(t, mi));
                                        isValidCall = false;
                                    }else if (mi.ReturnType!=typeof(void))
                                    {
                                        log?.LogTrace("Model {} is not valid because the method {} is using the AddItem delegate requires a void response",  t.FullName, mi.Name);
                                        invalidModels.Add(t);
                                        errors.Add(new MethodWithAddItemNotVoid(t, mi));
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
            invalidModels = invalidModels.Distinct().ToList();
            return errors;
        }
    }
}
