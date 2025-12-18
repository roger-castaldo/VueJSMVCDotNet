using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VueJSMVCDotNet.Attributes.ModelHandlers;
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
        public bool IsSlow => method.GetCustomAttributes().OfType<ExposedMethodAttribute>().Any(em => em.IsSlow);
        public bool RequiresModel => Array.Exists(parameters, p => p.GetCustomAttribute<ModelIDParameterAttribute>()!=null
                    || p.GetCustomAttribute<ModelInstanceParameterAttribute>()!=null);
        public bool UsesModel => Array.Exists(parameters, p => p.GetCustomAttribute<ModelInstanceParameterAttribute>()!=null);
        public Type ReturnType { get; private init; }
        public bool IsArrayReturn { get; private init; }
        public ASecurityCheckAttribute[] SecurityChecks { get; private init; }

        private readonly int addItemIndex;
        private readonly bool isTask;
        private readonly bool isValueTask;
        public bool HasAddItem => addItemIndex!=-1;
        private readonly ParameterInfo[] parameters;
        private readonly (ParameterInfo ParameterInfo, bool IsStrippable)[] strippedParameters;
        public NotNullArguementAttribute? NotNullArguement { get; private init; }
        public ParameterInfo[] StrippedParameters { get; private init; }

        public InjectableMethod(MethodInfo method, ASecurityCheckAttribute[] securityChecks)
        {
            this.method = method;
            (ReturnType, IsArrayReturn, _, isTask, isValueTask) = Utility.ExtractUnderlyingType(method.ReturnType);
            NotNullArguement = method.GetCustomAttribute<NotNullArguementAttribute>();
            parameters = this.method.GetParameters();
            var addIndex = parameters.IndexOf(p => p.ParameterType==typeof(AddItem));
            strippedParameters = StripMethodParameters(parameters).ToArray();
            StrippedParameters = strippedParameters.Where(p => !p.IsStrippable).Select(p => p.ParameterInfo).ToArray();
            addItemIndex = addIndex;
            SecurityChecks=securityChecks;
        }

        public async Task<T?> InvokeAsync<T, M>(IModelHandler<M> handler, IInternalRequestData? requestData, ILogger? logger, object?[]? pars = null, AddItem? addItem = null, M? modelInstance = default)
            where M : IModel
        {
            ArgumentNullException.ThrowIfNull(requestData);
            object?[] mpars = new object[parameters.Length];
            if (addItemIndex!=-1)
                mpars[addItemIndex] = addItem;
            List<int> ignoredIndexes = [addItemIndex];
            (mpars,ignoredIndexes) = await LoadMethodParametersAsync<M>(mpars, addItemIndex, ignoredIndexes, pars, requestData, handler, modelInstance);
            object? result = null;
            if (isTask||isValueTask)
                result = await InvokeMethodAsync(handler, mpars);
            else
                result = method.Invoke(handler, mpars);
            return (ReturnType==typeof(void) ? default(T?) : (T?)result);
        }

        public async Task<(T? result, IInternalRequestData requestData)> InvokeAsync<T, M>(IModelHandler<M> handler, HttpContext httpContext, ILogger? logger, object?[]? pars = null, AddItem? addItem = null, M? modelInstance = default)
            where M : IModel
        {
            var requestData = await Helper.ExtractPartsAsync(httpContext, logger);
            return (await InvokeAsync<T, M>(handler, requestData, logger, pars: pars, addItem: addItem, modelInstance: modelInstance), requestData);
        }

        private async Task<object?> InvokeMethodAsync<M>(IModelHandler<M> handler, object?[] mpars) where M : IModel
        {
            Task task;
            if (isTask)
                task = (Task)method.Invoke(handler, mpars)!;
            else
            {
                var vtask = method.Invoke(handler, mpars)!;
                task = (Task)vtask.GetType().GetMethod(nameof(ValueTask.AsTask))!.Invoke(vtask, null)!;
            }
            await task;
            if (ReturnType!=typeof(void))
                return task.GetType().GetProperty("Result")!.GetValue(task);
            return null;
        }

        private async Task<(object?[],List<int>)> LoadMethodParametersAsync<M>(object?[] mpars, int addItemIndex, List<int> ignoredIndexes, object?[] pars, IInternalRequestData requestData, IModelHandler<M> handler, M? modelInstance)
            where M : IModel
        {
            int index = 0;
            for (int x = 0; x<mpars.Length; x++)
            {
                if (x==addItemIndex)
                    continue;
                if (strippedParameters[x].IsStrippable)
                {
                    ignoredIndexes.Add(x);
                    mpars[x] = (strippedParameters[x].ParameterInfo.GetCustomAttribute<ModelIDParameterAttribute>(), strippedParameters[x].ParameterInfo.GetCustomAttribute<ModelInstanceParameterAttribute>()) switch
                    {
                        (not null,_)=>requestData.ModelID,
                        (null,not null)=> modelInstance ?? (requestData.ModelID!=null ? (await handler.LoadAsync(requestData.ModelID!)) : default),
                        (_,_)=> requestData[parameters[x].ParameterType]
                    };
                }
                else
                {
                    mpars[x]=pars?[index];
                    index++;
                }
            }
            return (mpars, ignoredIndexes);
        }
    }
}
