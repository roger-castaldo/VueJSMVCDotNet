using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet
{
    internal static class DefinitionValidator
    {
        private readonly struct SPathTypePair
        {
            public string Path { get; init; }
            public Type ModelType { get; init; }
        }

        private static bool IsValidDataActionMethod(MethodInfo method)
            => (method.ReturnType == typeof(bool)) && !InjectableMethod.StripMethodParameters(method).Any();

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
        internal static IEnumerable<Exception> Validate(AssemblyLoadContext alc, ILogger? log, out IEnumerable<Type> invalidModels, out IEnumerable<Type> models)
        {
            log?.LogDebug("Attempting to load and validate the models found in the Assembly Load Context {Name}", alc.Name);
            models = Utility.LocateTypeInstances(typeof(IModel), alc, log);
            log?.LogDebug("Located {Count} models in Assembly Load Context {Name}", models?.Count(), alc.Name);
            List<Exception> errors = [];
            List<SPathTypePair> paths = [];
            foreach (Type t in (models?? []))
            {
                log?.LogDebug("Validating Model {FullName}", t.FullName);
                //validate routing
                if (t.GetCustomAttribute<ModelRouteAttribute>(false)==null)
                {
                    log?.LogTrace("Model {FullName} has no route", t.FullName);
                    errors.Add(new NoRouteException(t));
                }
                t.GetCustomAttributes<ModelRouteAttribute>(false)
                    .ForEach(mr =>
                    {
                        Regex reg = new("^(" + (mr.Host == "*" ? ".+" : mr.Host) + (mr.Path.StartsWith('/') ? mr.Path : "/" + mr.Path) + ")$", RegexOptions.ECMAScript | RegexOptions.Compiled);
                        foreach (SPathTypePair p in paths)
                        {
                            if (reg.IsMatch(p.Path) && (p.ModelType.FullName != t.FullName))
                            {
                                log?.LogTrace("Model {FullName} has a model route that is a duplicate of another model", t.FullName);
                                errors.Add(new DuplicateRouteException(p.Path, p.ModelType, mr.Host + (mr.Path.StartsWith('/') ? mr.Path : $"/{mr.Path}"), t));
                            }
                        }
                        paths.Add(new()
                        {
                            Path=mr.Host + (mr.Path.StartsWith('/') ? mr.Path : "/" + mr.Path),
                            ModelType=t
                        });
                    });

                //validate core properties
                if (t.GetProperty("id")?.GetCustomAttributes(typeof(ModelIgnorePropertyAttribute), false).Length > 0)
                {
                    log?.LogTrace("Model {TypeName} is not valid because the id property is blocked by ModelIgnoreProperty", t.FullName);
                    errors.Add(new ModelIDBlockedException(t));
                }

                errors.AddRange(CheckModelMethods(t, log));
                errors.AddRange(CheckLoadTypeMethods(t, log));
                errors.AddRange(CheckExposedMethods(t, log));
            }
            invalidModels = errors.OfType<ModelTypeException>().Select(e => e.ModelType).Distinct();
            return errors;
        }

        private static IEnumerable<Exception> CheckExposedMethods(Type t, ILogger? log)
        {
            List<Exception> errors = [];
            List<string> methods = [];
            (new BindingFlags[] { Constants.STATIC_INSTANCE_METHOD_FLAGS, Constants.INSTANCE_METHOD_FLAGS })
                .SelectMany(bf => t.GetMethods(bf))
                .Select(mi => new { Method = mi, ExposedAttribute = mi.GetCustomAttribute<ExposedMethodAttribute>(false) })
                .Where(ms => ms.ExposedAttribute != null)
                .ForEach(ms =>
                {
                    var im = new InjectableMethod(ms.Method);
                    bool hasAddItem = im.HasAddItem;
                    int parCount = im.StrippedParameters.Length;
                    if (methods.Contains($"{ms.Method.Name}.{parCount}"))
                    {
                        log?.LogTrace("Model {TypeName} is not valid because the method {MethodName} has a duplicate method signature", t.FullName, ms.Method.Name);
                        errors.Add(new DuplicateMethodSignatureException(t, ms.Method));
                    }
                    else if (ms.ExposedAttribute!=null)
                    {
                        bool isValidCall = true;
                        if (hasAddItem)
                        {
                            if (!ms.ExposedAttribute.IsSlow)
                            {
                                log?.LogTrace("Model {TypeName} is not valid because the method {MethodName} is using the AddItem delegate but is not marked slow", t.FullName, ms.Method.Name);
                                errors.Add(new MethodNotMarkedAsSlow(t, ms.Method));
                                isValidCall = false;
                            }
                            else if (ms.Method.ReturnType!=typeof(void))
                            {
                                log?.LogTrace("Model {TypeName} is not valid because the method {MethodName} is using the AddItem delegate requires a void response", t.FullName, ms.Method.Name);
                                errors.Add(new MethodWithAddItemNotVoid(t, ms.Method));
                                isValidCall = false;
                            }
                        }
                        if (isValidCall)
                            methods.Add($"{ms.Method.Name}.{parCount}");
                    }
                });
            return errors;
        }

        private static IEnumerable<Exception> CheckLoadTypeMethods(Type t, ILogger? log)
        {
            List<Exception> errors = [];
            bool found = false;
            bool foundLoadAll = false;
            t.GetMethods(Constants.LOAD_METHOD_FLAGS)
                .Select(mi =>
                new
                {
                    Method = mi,
                    IsLoad = mi.GetCustomAttribute<ModelLoadMethodAttribute>(false)!=null,
                    IsLoadAll = mi.GetCustomAttribute<ModelLoadAllMethodAttribute>(false)!=null,
                    ListMethod = mi.GetCustomAttribute<ModelListMethodAttribute>(false)
                })
                .Where(ms => ms.IsLoad||ms.IsLoadAll||ms.ListMethod!=null)
                .ForEach(ms =>
                {
                    if (ms.IsLoad && found)
                    {
                        log?.LogTrace("Model {FullName} has a duplicated load method", t.FullName);
                        errors.Add(new DuplicateLoadMethodException(t, ms.Method));
                    }
                    else if (ms.IsLoad && !found)
                    {
                        var rtype = Utility.ExtractUnderlyingType(ms.Method.ReturnType, out var isArray, out _, out _);
                        if ((rtype != t || isArray)&& !ms.Method.ReturnType.IsAssignableFrom(t))
                        {
                            log?.LogTrace("Model {FullName} does not return a valid type for its Load method", t.FullName);
                            errors.Add(new InvalidLoadMethodReturnType(t, ms.Method));
                        }
                        if (rtype == t)
                        {
                            var pars = InjectableMethod.StripMethodParameters(ms.Method);
                            if (pars.Count()==1 && pars.First().ParameterType==typeof(string))
                                found = true;
                            else
                            {
                                log?.LogTrace("Model {FullName} has an invalid load method", t.FullName);
                                errors.Add(new InvalidLoadMethodArguements(t, ms.Method));
                            }
                        }
                    }
                    else if (ms.IsLoadAll && foundLoadAll)
                    {
                        log?.LogTrace("Model {FullName} has more than 1 ModelLoadAllMethod", t.FullName);
                        errors.Add(new DuplicateLoadAllMethodException(t, ms.Method));
                    }
                    else if (ms.IsLoadAll && !foundLoadAll)
                    {
                        var rtype = Utility.ExtractUnderlyingType(ms.Method.ReturnType, out var isArray, out _, out _);
                        if (!isArray)
                        {
                            rtype=null;
                            log?.LogTrace("Model {FullName} has an invalid return type for ModelLoadAllMethod", t.FullName);
                            errors.Add(new InvalidLoadAllMethodReturnType(t, ms.Method));
                        }
                        if (rtype!=null)
                        {
                            if (rtype!=t)
                            {
                                log?.LogTrace("Model {FullName} has an invalid return type for ModelLoadAllMethod", t.FullName);
                                errors.Add(new InvalidLoadAllMethodReturnType(t, ms.Method));
                            }
                            else
                            {
                                var pars = InjectableMethod.StripMethodParameters(ms.Method);
                                if (pars.Count()!=0)
                                    errors.Add(new InvalidLoadAllArguements(t, ms.Method));
                                else
                                    foundLoadAll=true;
                            }
                        }
                    }
                    else if (ms.ListMethod!=null)
                    {
                        var rtype = Utility.ExtractUnderlyingType(ms.Method.ReturnType, out var isArray, out _, out _);
                        if (rtype != t || !isArray)
                        {
                            log?.LogTrace("Model {FullName} has an invalid return type for the model list method {Name}", t.FullName, ms.Method.Name);
                            errors.Add(new InvalidModelListMethodReturnException(t, ms.Method));
                        }
                        var pars = InjectableMethod.StripMethodParameters(ms.Method);
                        if (ms.ListMethod.Paged && pars.Count()<3)
                        {
                            log?.LogTrace("Model {FullName} has an invalid signature for paged model list method {Name}, required parameters are missing", t.FullName, ms.Method.Name);
                            errors.Add(new InvalidModelListParameterCountException(t, ms.Method));
                        }
                        for (int x = 0; x < pars.Count(); x++)
                        {
                            ParameterInfo pi = pars.ElementAt(x);
                            if (pi.IsOut && (!ms.ListMethod.Paged || x != pars.Count() - 1))
                            {
                                log?.LogTrace("Model {TypeName} list method {MethodName} with the parameter {ParameterName}", t.FullName, ms.Method.Name, pi.Name);
                                errors.Add(new InvalidModelListParameterOutException(t, ms.Method, pi));
                            }
                            if (ms.ListMethod.Paged && x >= pars.Count() - 3)
                            {
                                Type ptype = pi.ParameterType;
                                if (pi.IsOut)
                                    ptype = ptype.GetElementType()!;
                                if (ptype != typeof(int)
                                    && ptype != typeof(long)
                                    && ptype != typeof(short)
                                    && ptype != typeof(uint)
                                    && ptype != typeof(ulong)
                                    && ptype != typeof(ushort))
                                {
                                    log?.LogTrace("Model {TypeName} has an invalid parameter {ParameterName} list method {MethodName}", t.FullName, pi.Name, ms.Method.Name);
                                    errors.Add(new InvalidModelListPageParameterTypeException(t, ms.Method, pi));
                                }
                            }
                            if (ms.ListMethod.Paged && x == pars.Count() - 1 && !pi.IsOut)
                            {
                                log?.LogTrace("Model {TypeName} is not a valid page total parameter {ParameterName} list method {MethodName}", t.FullName, pi.Name, ms.Method.Name);
                                errors.Add(new InvalidModelListPageTotalPagesNotOutException(t, ms.Method, pi));
                            }
                        }
                    }
                });
            if (!found)
            {
                log?.LogTrace("Model {TypeName} is not valid because no load method was found", t.FullName);
                errors.Add(new NoLoadMethodException(t));
            }
            return errors;
        }

        private static IEnumerable<Exception> CheckModelMethods(Type t, ILogger? log)
        {
            List<Exception> errors = [];
            bool hasAdd = false;
            bool hasUpdate = false;
            bool hasDelete = false;
            t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS)
                .Select(mi =>
                new
                {
                    Method = mi,
                    IsSave = mi.GetCustomAttribute<ModelSaveMethodAttribute>(false)!=null,
                    IsDelete = mi.GetCustomAttribute<ModelDeleteMethodAttribute>(false)!=null,
                    IsUpdate = mi.GetCustomAttribute<ModelUpdateMethodAttribute>(false)!=null
                })
                .Where(ms => ms.IsSave||ms.IsDelete||ms.IsUpdate)
                .ForEach(ms =>
                {
                    if (ms.IsSave && hasAdd)
                    {
                        log?.LogTrace("Model {FullName} has more than 1 save method", t.FullName);
                        errors.Add(new DuplicateModelSaveMethodException(t, ms.Method));
                    }
                    else if (ms.IsSave && !hasAdd)
                    {
                        hasAdd = true;
                        if (!IsValidDataActionMethod(ms.Method))
                        {
                            log?.LogTrace("Model {FullName} has and invalid save method", t.FullName);
                            errors.Add(new InvalidModelSaveMethodException(t, ms.Method));
                        }
                        if (t.GetConstructor(Type.EmptyTypes) == null)
                        {
                            log?.LogTrace("Model {FullName} has a save method without an empty constructor", t.FullName);
                            errors.Add(new NoEmptyConstructorException(t));
                        }
                    }
                    else if (ms.IsUpdate && hasUpdate)
                    {
                        log?.LogTrace("Model {FullName} has more than 1 update method", t.FullName);
                        errors.Add(new DuplicateModelUpdateMethodException(t, ms.Method));
                    }
                    else if (ms.IsUpdate && !hasUpdate)
                    {
                        hasUpdate = true;
                        if (!IsValidDataActionMethod(ms.Method))
                        {
                            log?.LogTrace("Model {FullName} has and invalid update method", t.FullName);
                            errors.Add(new InvalidModelUpdateMethodException(t, ms.Method));
                        }
                    }
                    else if (ms.IsDelete && hasDelete)
                    {
                        log?.LogTrace("Model {FullName} has more than 1 delete method", t.FullName);
                        errors.Add(new DuplicateModelDeleteMethodException(t, ms.Method));
                    }
                    else if (ms.IsDelete && !hasDelete)
                    {
                        hasDelete = true;
                        if (!IsValidDataActionMethod(ms.Method))
                        {
                            log?.LogTrace("Model {FullName} has and invalid delete method", t.FullName);
                            errors.Add(new InvalidModelDeleteMethodException(t, ms.Method));
                        }
                    }
                });
            return errors;
        }
    }
}
