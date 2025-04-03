using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Extensions;
using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.Interfaces.Internal;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal class InjectableMethod
    {
        private static readonly Type[] StrippableTypes = [typeof(AddItem), typeof(HttpContext), typeof(IFormFile), typeof(IReadOnlyList<IFormFile>)];

        public static IEnumerable<(ParameterInfo ParameterInfo, bool IsStrippable)> StripMethodParameters(ParameterInfo[] parameters)
            => parameters
                .Select((p, index) =>
                (
                    p,
                    StrippableTypes.Contains(p.ParameterType)
                    || p.ParameterType.IsInterface
                    || p.GetCustomAttribute<FromServicesAttribute>()!=null
                    || p.GetCustomAttribute<ModelIDParameterAttribute>()!=null
                    || p.GetCustomAttribute<ModelInstanceParameterAttribute>()!=null
                ));

        private readonly MethodInfo method;
        public MethodInfo Method => method;
        public string Name => method.Name;
        public bool IsModelUpdateOrSave => method.GetCustomAttributes().Any(att => att is ModelUpdateMethodAttribute || att is ModelSaveMethodAttribute);
        public bool IsSlow => method.GetCustomAttributes().OfType<ExposedMethodAttribute>().Any(em => em.IsSlow);
        public bool RequiresModel => Array.Exists(parameters, p => p.GetCustomAttribute<ModelIDParameterAttribute>()!=null
                    || p.GetCustomAttribute<ModelInstanceParameterAttribute>()!=null);
        public bool UsesModel => Array.Exists(parameters, p => p.GetCustomAttribute<ModelInstanceParameterAttribute>()!=null);
        public Type ReturnType { get; private init; }
        public bool IsArrayReturn { get; private init; }

        public IEnumerable<Attribute> GetCustomAttributes() => method.GetCustomAttributes();

        private readonly int addItemIndex;
        private readonly bool isTask;
        public bool HasAddItem => addItemIndex!=-1;
        private readonly ParameterInfo[] parameters;
        private readonly IEnumerable<ASecurityCheckAttribute> securityChecks;
        private readonly (ParameterInfo ParameterInfo, bool IsStrippable)[] strippedParameters;
        public NotNullArguementAttribute? NotNullArguement { get; private init; }
        public ParameterInfo[] StrippedParameters { get; private init; }

        public InjectableMethod(MethodInfo method)
        {
            this.method = method;
            ReturnType = Utility.ExtractUnderlyingType(method.ReturnType, out var isArray, out _, out isTask);
            IsArrayReturn=isArray;
            NotNullArguement = method.GetCustomAttribute<NotNullArguementAttribute>();
            parameters = this.method.GetParameters();
            var addIndex = parameters.IndexOf(p => p.ParameterType==typeof(AddItem));
            strippedParameters = StripMethodParameters(parameters).ToArray();
            StrippedParameters = strippedParameters.Where(p => !p.IsStrippable).Select(p => p.ParameterInfo).ToArray();
            addItemIndex = addIndex;
            securityChecks = method.DeclaringType!.GetCustomAttributes().OfType<ASecurityCheckAttribute>()
                .Concat(method.GetCustomAttributes().OfType<ASecurityCheckAttribute>());
        }

        public async Task<bool> HasValidAccess(IRequestData data, IModel? model, string url, string? id)
        {
            foreach (var sc in securityChecks)
            {
                if (!await sc.HasValidAccessAsync(data, model, url, id))
                    return false;
            }
            return true;
        }

        public async Task<(T? result, IInternalRequestData requestData)> InvokeAsync<T, M>(IModelHandler<M> handler, HttpContext httpContext, ILogger? logger, object?[]? pars = null, AddItem? addItem = null, M? modelInstance = default)
            where M : IModel
        {
            var requestData = await Helper.ExtractPartsAsync(httpContext, logger);
            ArgumentNullException.ThrowIfNull(requestData);
            object?[] mpars = new object[parameters.Length];
            if (addItemIndex!=-1)
                mpars[addItemIndex] = addItem;
            List<int> ignoredIndexes = [addItemIndex];
            int index = 0;
            for (int x = 0; x<mpars.Length; x++)
            {
                if (x!=addItemIndex)
                {
                    if (strippedParameters[x].IsStrippable)
                    {
                        ignoredIndexes.Add(x);
                        if (strippedParameters[x].ParameterInfo.GetCustomAttribute<ModelIDParameterAttribute>()!=null)
                            mpars[x] = requestData.ModelID;
                        else if (strippedParameters[x].ParameterInfo.GetCustomAttribute<ModelInstanceParameterAttribute>()!=null)
                            mpars[x] = modelInstance ?? (requestData.ModelID!=null ? (await handler.LoadAsync(requestData.ModelID!)) : default);
                        else
                            mpars[x] = requestData[parameters[x].ParameterType];
                    }
                    else
                    {
                        mpars[x]=pars?[index];
                        index++;
                    }
                }
            }
            object? result;
            if (isTask)
            {
                var task = (Task)method.Invoke(handler, mpars)!;
                await task;
                if (task.Exception!=null)
                    throw task.Exception;
                result=task.GetType().GetProperty("Result")!.GetValue(task);
            }
            else
                result = method.Invoke(handler, mpars);
            if (Array.Exists(parameters, p => p.IsOut))
            {
                index = 0;
                for (int x = 0; x<parameters.Length; x++)
                {
                    if (!ignoredIndexes.Contains(x))
                    {
                        if (parameters[x].IsOut)
                            pars![index]=mpars[x];
                        index++;
                    }
                }
            }
            return ((ReturnType==typeof(void) ? default(T?) : (T?)result), requestData);
        }
    }
}
