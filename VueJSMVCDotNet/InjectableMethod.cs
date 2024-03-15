using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet
{
    internal class InjectableMethod
    {
        private readonly MethodInfo method;
        public string Name => method.Name;
        public bool IsModelUpdateOrSave => method.GetCustomAttributes().Any(att => att is ModelUpdateMethod || att is ModelSaveMethod);
        public bool IsSlow =>method.GetCustomAttributes().OfType<ExposedMethod>().Any(em => em.IsSlow);
        public Type ReturnType { get; private init; }
        public bool IsArrayReturn { get; private init; }

        public IEnumerable<Attribute> GetCustomAttributes()
        {
            return method.GetCustomAttributes();
        }

        private readonly int secureSessionIndex;
        private readonly int addItemIndex;
        private readonly bool isTask;
        public bool HasAddItem => addItemIndex!=-1;
        private readonly int loggerIndex;
        private readonly int headerIndex;
        private readonly ParameterInfo[] parameters;
        private readonly IEnumerable<ASecurityCheck> securityChecks;
        private readonly ILogger log;
        public NotNullArguement NotNullArguement { get; private init; }
        public ParameterInfo[] StrippedParameters { get; private init; }

        public InjectableMethod(MethodInfo method,ILogger log)
        {
            this.method = method;
            this.log=log;
            ReturnType = Utility.ExtractUnderlyingType(method.ReturnType, out var isArray, out _, out isTask);
            IsArrayReturn=isArray;
            NotNullArguement = (NotNullArguement)method.GetCustomAttribute(typeof(NotNullArguement));
            parameters = this.method.GetParameters();
            List<ParameterInfo> strippedPars = new();
            secureSessionIndex=-1;
            addItemIndex=-1;
            loggerIndex=-1;
            headerIndex=-1;
            for(int x = 0; x<parameters.Length; x++)
            {
                if (parameters[x].ParameterType == typeof(ISecureSession) ||
                new List<Type>(parameters[x].ParameterType.GetInterfaces()).Contains(typeof(ISecureSession)))
                    secureSessionIndex=x;
                else if (parameters[x].ParameterType==typeof(AddItem))
                    addItemIndex=x;
                else if (parameters[x].ParameterType==typeof(ILogger))
                    loggerIndex=x;
                else if (parameters[x].ParameterType==typeof(IHeaderDictionary))
                    headerIndex=x;
                else if (parameters[x].ParameterType==typeof(IFormFile)
                    || parameters[x].ParameterType==typeof(IReadOnlyList<IFormFile>)
                    || !parameters[x].ParameterType.IsInterface)
                    strippedPars.Add(parameters[x]);
            }
            StrippedParameters= strippedPars.ToArray();
            securityChecks = method.DeclaringType.GetCustomAttributes().OfType<ASecurityCheck>()
                .Concat(method.GetCustomAttributes().OfType<ASecurityCheck>());
        }

        public async Task<bool> HasValidAccess(IRequestData data, IModel model, string url, string id)
        {
            foreach(var sc in securityChecks)
            {
                if (!await sc.HasValidAccessAsync(data, model, url, id))
                    return false;
            }
            return true;
        }

        public async Task<object> InvokeAsync(object obj, IRequestData requestData, object[] pars = null, AddItem addItem = null, IHeaderDictionary responseHeaders=null)
        {
            if (requestData==null)
                throw new ArgumentNullException(nameof(requestData));
            object[] mpars = new object[parameters.Length];
            if (secureSessionIndex!=-1)
                mpars[secureSessionIndex] = requestData.Session;
            if (addItemIndex!=-1)
                mpars[addItemIndex] = addItem;
            if (loggerIndex!=-1)
                mpars[loggerIndex] = log;
            if (headerIndex!=-1)
                mpars[headerIndex] = responseHeaders;
            var ignoredIndexes = new List<int>(new int[] { secureSessionIndex, addItemIndex, loggerIndex, headerIndex });
            int index = 0;
            for (int x = 0; x<mpars.Length; x++)
            {
                if (x!=secureSessionIndex&&x!=addItemIndex&&x!=loggerIndex&&x!=headerIndex)
                {
                    if (parameters[x].ParameterType.IsInterface
                        && !(
                            parameters[x].ParameterType==typeof(IFormFile)
                            || parameters[x].ParameterType==typeof(IReadOnlyList<IFormFile>)
                        )
                    )
                    {
                        ignoredIndexes.Add(x);
                        mpars[x]=requestData[parameters[x].ParameterType];
                    }
                    else
                    {
                        mpars[x]=pars[index];
                        index++;
                    }
                }
            }
            object? result;
            if (isTask)
            {
                var task = (Task)method.Invoke(obj, mpars);
                await task;
                if (task.Exception!=null)
                    throw task.Exception;
                result=task.GetType().GetProperty("Result").GetValue(task);
            }
            else
                result = method.Invoke(obj, mpars);
            if (parameters.Any(p => p.IsOut))
            {
                index = 0;
                for (int x = 0; x<parameters.Length; x++)
                {
                    if (!ignoredIndexes.Contains(x))
                    {
                        if (parameters[x].IsOut)
                            pars[index]=mpars[x];
                        index++;
                    }
                }
            }
            return (ReturnType==typeof(void) ? null : result);
        }
    }
}
