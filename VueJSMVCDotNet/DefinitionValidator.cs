using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Attributes.Models;
using VueJSMVCDotNet.Endpoints.Model;
using VueJSMVCDotNet.Extensions;
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

        private static bool IsValidDataActionMethod(MethodInfo method, Type returnType, bool requiresInstance)
        {
            var iMethod = new InjectableMethod(method, []);
            return Equals(returnType, iMethod.ReturnType)
                && iMethod.StrippedParameters.Length==0
                && iMethod.RequiresModel
                && (!requiresInstance || iMethod.UsesModel);
        }

        private static void AppendError(List<Exception> exceptions, ILogger? log, Exception error, string logMessage, params object?[] args)
        {
            log?.LogTrace(logMessage, args);
            exceptions.Add(error);
        }

        /*
         * Called to validate all model definitions through the following checks:
         * 1.  Check to make sure that there is at least 1 route specified for the handler.
         * 2.  Check to make sure that the id property is not blocked.
         * 3.  Check to make sure all exposed slow methods are valid (ensure they have a parameter for the AddItem delegate and their response is void or Task)
         * 4.  Check to make sure all exposed methods have a unique signature
         * 5.  Check to make sure that the select model method has the right return type
         * 6.  Check to make sure the save, update and delete methods are valid, if defined
         */
        internal static IEnumerable<Exception> Validate(AssemblyLoadContext alc, ILogger? log, out IEnumerable<(Type HandlerType, Type ModelType)> invalidModels, out IEnumerable<(Type HandlerType, Type ModelType)> models)
        {
            log?.LogDebug("Attempting to load and validate the models found in the Assembly Load Context {Name}", alc.Name);
            var handlers = Utility.LocateModelHandlers(alc, log);
            log?.LogDebug("Located {Count} models in Assembly Load Context {Name}", handlers.Count(), alc.Name);
            var errors = handlers
                .SelectMany(handler =>
                {
                    log?.LogDebug("Validating Handler {FullName}", handler.HandlerType.FullName);
                    var exceptions = new List<Exception>();
                    if (handler.HandlerType.GetCustomAttribute<ModelRouteAttribute>(false)==null)
                        AppendError(exceptions, log, new NoRouteException(handler.HandlerType), "Model {FullName} has no route", handler.HandlerType.FullName);
                    else
                    {
                        foreach (var altHandler in handlers.Where(h =>
                            !Equals(handler.HandlerType, h.HandlerType)
                            && string.Equals(handler.HandlerType.GetCustomAttribute<ModelRouteAttribute>(false)?.Path, h.HandlerType.GetCustomAttribute<ModelRouteAttribute>(false)?.Path??string.Empty, StringComparison.OrdinalIgnoreCase))
                            .Select(h=>h.HandlerType)
                        )
                            AppendError(exceptions, log, new DuplicateRouteException(
                                handler.HandlerType.GetCustomAttribute<ModelRouteAttribute>(false)!.Path,
                                handler.HandlerType,
                                altHandler.GetCustomAttribute<ModelRouteAttribute>(false)!.Path,
                                altHandler
                                ), "Model {FullName} has a model route that is a duplicate of another model", handler.HandlerType.FullName);
                    }
                    if (handler.ModelType.GetProperty(nameof(IModel.id))!.GetCustomAttribute<ModelIgnorePropertyAttribute>()!=null)
                        AppendError(exceptions, log, new ModelIDBlockedException(handler.ModelType), "Model {TypeName} is not valid because the id property is blocked by ModelIgnoreProperty", handler.ModelType.FullName);

                    var methods = handler.HandlerType.GetMethods(Constants.METHOD_FLAGS);

                    CheckExposedMethods(methods, handler, exceptions, log);
                    CheckLoadAllMethod(methods, handler, exceptions, log);
                    CheckListMethods(methods, handler, exceptions, log);
                    CheckModelMethod<ModelSaveMethodAttribute>(methods, handler, exceptions, log, "save", typeof(string), true,
                        (type, method) => new DuplicateModelSaveMethodException(type, method),
                        (type, method) => new InvalidModelSaveMethodException(type, method)
                    );
                    CheckModelMethod<ModelUpdateMethodAttribute>(methods, handler, exceptions, log, "update", typeof(bool), true,
                        (type, method) => new DuplicateModelUpdateMethodException(type, method),
                        (type, method) => new InvalidModelUpdateMethodException(type, method)
                    );
                    CheckModelMethod<ModelDeleteMethodAttribute>(methods, handler, exceptions, log, "delete", typeof(bool), false,
                        (type, method) => new DuplicateModelDeleteMethodException(type, method),
                        (type, method) => new InvalidModelDeleteMethodException(type, method)
                    );

                    return exceptions;
                });
            invalidModels = errors.OfType<ModelTypeException>()
                .Select(e => handlers.First(h => Equals(h.HandlerType, e.ModelType) || Equals(h.ModelType, e.ModelType)))
                .Distinct();
            models = handlers;
            return errors;
        }

        private static IEnumerable<MethodInfo> CheckModelMethod<MA>(MethodInfo[] methods,
            (Type HandlerType, Type ModelType) handler, List<Exception> exceptions, ILogger? log,
            string methodType, Type returnType, bool requiresInstance,
            Func<Type, MethodInfo, ModelTypeMethodException> constructDuplicateException,
            Func<Type, MethodInfo, ModelTypeMethodException> constructInvalidException)
            where MA : Attribute
        {
            var filteredMethods = methods.Where(mi => mi.GetCustomAttribute<MA>(false)!=null);
            if (filteredMethods.Count()>1)
                filteredMethods.ForEach(mi => AppendError(exceptions, log, constructDuplicateException(handler.HandlerType, mi),
                    $"Handler {{FullName}} has more than 1 {methodType} method", handler.HandlerType.FullName));
            else if (filteredMethods.Count()==1 && !IsValidDataActionMethod(filteredMethods.First(), returnType, requiresInstance))
                AppendError(exceptions, log, constructInvalidException(handler.HandlerType, filteredMethods.First()),
                    $"Handler {{FullName}} has and invalid {methodType} method", handler.HandlerType.FullName);
            return filteredMethods;
        }

        private static void CheckExposedMethods(MethodInfo[] methods, (Type HandlerType, Type ModelType) handler, List<Exception> exceptions, ILogger? log)
            => methods.Select(mi => new { Method = mi, ExposedAttribute = mi.GetCustomAttribute<ExposedMethodAttribute>(false) })
                        .Where(ms => ms.ExposedAttribute!=null)
                        .Select(ms => new {Method = new InjectableMethod(ms.Method, []), ExposedAttribute=ms.ExposedAttribute})
                        .GroupBy(ms => $"{(ms.Method.RequiresModel ? "instance" : "static")}:{ms.Method.Name}({string.Join(',',ms.Method.StrippedParameters.Select(p=>p.Name))})")
                        .ForEach(grp =>
                        {
                            if (grp.Count()>1)
                            {
                                grp.ForEach(ms =>
                                {
                                    AppendError(exceptions, log, new DuplicateMethodSignatureException(handler.HandlerType, ms.Method.Method),
                                        "Handler {FullName} has a duplicate method signature for the method {MethodName}", handler.HandlerType.FullName, ms.Method.Name);
                                });
                            }
                            grp.ForEach(ms =>
                            {
                                if (ms.Method.HasAddItem)
                                {
                                    if (!ms.ExposedAttribute!.IsSlow)
                                        AppendError(exceptions, log, new MethodNotMarkedAsSlow(handler.HandlerType, ms.Method.Method),
                                            "Model {TypeName} is not valid because the method {MethodName} is using the AddItem delegate but is not marked slow", handler.HandlerType.FullName, ms.Method.Name);
                                    else if (ms.Method.ReturnType!=typeof(void))
                                        AppendError(exceptions, log, new MethodWithAddItemNotVoid(handler.HandlerType, ms.Method.Method),
                                            "Model {TypeName} is not valid because the method {MethodName} is using the AddItem delegate requires a void response", handler.HandlerType.FullName, ms.Method.Name);
                                }
                            });
                        });

        private static void CheckLoadAllMethod(MethodInfo[] methods, (Type HandlerType, Type ModelType) handler, List<Exception> exceptions, ILogger? log)
        {
            var filteredMethods = methods.Where(mi => mi.GetCustomAttribute<ModelLoadAllMethodAttribute>(false)!=null);
            if (filteredMethods.Count()>1)
                filteredMethods.ForEach(mi => AppendError(exceptions, log, new DuplicateLoadAllMethodException(handler.HandlerType, mi),
                    "Handler {FullName} has more than 1 ModelLoadAllMethod", handler.HandlerType.FullName));
            filteredMethods.ForEach(loadAllMethod =>
            {
                var rtype = Utility.ExtractUnderlyingType(loadAllMethod.ReturnType, out var isArray, out _, out _);
                if (!isArray)
                    AppendError(exceptions, log, new InvalidLoadAllMethodReturnType(handler.HandlerType, loadAllMethod),
                        "Handler {FullName} has an invalid return type for ModelLoadAllMethod", handler.HandlerType.FullName);
                else if (!Equals(rtype, handler.ModelType))
                    AppendError(exceptions, log, new InvalidLoadAllMethodReturnType(handler.HandlerType, loadAllMethod),
                        "Handler {FullName} has an invalid return type for ModelLoadAllMethod", handler.HandlerType.FullName);
                else if (InjectableMethod.StripMethodParameters(loadAllMethod.GetParameters()).Any(pair => !pair.IsStrippable))
                    exceptions.Add(new InvalidLoadAllArguements(handler.HandlerType, loadAllMethod));
            });
        }

        private static void CheckListMethods(MethodInfo[] methods, (Type HandlerType, Type ModelType) handler, List<Exception> exceptions, ILogger? log)
            => methods.Where(mi => mi.GetCustomAttribute<ModelListMethodAttribute>(false)!=null)
            .ForEach(method =>
            {
                var paged = method.GetCustomAttribute<ModelListMethodAttribute>(false)!.Paged;
                var rtype = Utility.ExtractUnderlyingType(method.ReturnType, out var isArray, out _, out _);
                if (paged)
                {
                    if (!method.GetParameters().Any(par => par.GetCustomAttribute<PageStartIndexParameterAttribute>(false)!=null))
                        AppendError(exceptions, log, new Exception("Missing Start Index Parameter"),
                            "Handler {FullName} has an invalid signature for paged model list method {Name}, missing PageStartIndex parameter", handler.HandlerType.FullName, method.Name);
                    if (!method.GetParameters().Any(par => par.GetCustomAttribute<PageSizeParameterAttribute>(false)!=null))
                        AppendError(exceptions, log, new Exception("Missing Page Size Parameter"),
                            "Handler {FullName} has an invalid signature for paged model list method {Name}, missing PageSize parameter", handler.HandlerType.FullName, method.Name);
                    if (!Equals(rtype, typeof(PagedResult<>).MakeGenericType(handler.ModelType)))
                        AppendError(exceptions, log, new Exception("Invalid Page return type"),
                            "Handler {FullName} has an invalid signature for paged model list method {Name}, Return Type expected to be PagedResult<M>", handler.HandlerType.FullName, method.Name);
                }
                else if (!Equals(rtype, handler.ModelType) || !isArray)
                    AppendError(exceptions, log, new InvalidModelListMethodReturnException(handler.HandlerType, method),
                        "Handler {FullName} has an invalid return type for the model list method {Name}", handler.HandlerType.FullName, method.Name);
            });
    }
}
