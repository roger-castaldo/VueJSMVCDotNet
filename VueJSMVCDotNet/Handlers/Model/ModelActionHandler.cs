using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class ModelActionHandler<T> : IModelActionHandler where T : IModel
    {
        private readonly InjectableMethod loadMethod;
        private readonly IEnumerable<InjectableMethod> methods;
        private readonly ILogger log;
        private readonly delRegisterSlowMethodInstance registerSlowMethod;
        protected readonly string callType;

        public IEnumerable<string> BaseURLs { get; private init; } = Array.Empty<string>();
        public IEnumerable<string> MethodNames => methods.Select(m => m.Name).Distinct();
        public ModelActionHandler(string callType, delRegisterSlowMethodInstance delRegisterSlowMethod, ILogger log)
            : this(Array.Empty<MethodInfo>(), callType, delRegisterSlowMethod,log)
        { }

        public ModelActionHandler(MethodInfo method, string callType,delRegisterSlowMethodInstance delRegisterSlowMethod, ILogger log)
            : this(new MethodInfo[] {method }, callType, delRegisterSlowMethod, log)
        {}

        public ModelActionHandler(IEnumerable<MethodInfo> methods, string callType, delRegisterSlowMethodInstance delRegisterSlowMethod, ILogger log)
        {
            this.methods = methods.Select(m => new InjectableMethod(m, log));
            this.callType=callType;
            registerSlowMethod=delRegisterSlowMethod;
            this.log=log;
            BaseURLs = typeof(T)
               .GetCustomAttributes(typeof(ModelRoute), false)
               .Select(ca => ((ModelRoute)ca).Path)
               .OrderByDescending(p => p.Length);
            loadMethod = new InjectableMethod(typeof(T).GetMethods(Constants.LOAD_METHOD_FLAGS).FirstOrDefault(m => m.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0),log);
        }

        public async Task<IModel> Load(string url, ModelRequestData request, Func<string, string> extractID = null)
        {
            if (loadMethod != null)
            {
                var id = extractID == null ? url[(url.LastIndexOf("/") + 1)..] : extractID(url);
                if (!await loadMethod.HasValidAccess(request, null, url, id))
                    throw new InsecureAccessException();
                log?.LogTrace("Attempting to load model at url {}", url);
                var result = (IModel)(await loadMethod.InvokeAsync(null,request, pars:new object[] { id }));
                if (result != null)
                    return result;
            }
            throw new CallNotFoundException("Model Not Found");
        }

        public async Task Invoke(string url, ModelRequestData request, HttpContext context, Func<string, string> extractID = null, Func<IModel, ModelRequestData, IModel> processLoadedModel = null)
            => await Invoke(url,request, context,await Load(url, request, extractID: extractID),processLoadedModel: processLoadedModel);

        public async Task InvokeWithoutLoad(string url, ModelRequestData request, HttpContext context, IModel model=null, Func<IModel, object, object[], InjectableMethod, object> extractResponse = null)
            => await Invoke(url, request, context, model, extractResponse: extractResponse);

        private async Task Invoke(string url, ModelRequestData request, HttpContext context,IModel model,Func<IModel, ModelRequestData, IModel> processLoadedModel = null, Func<IModel, object, object[], InjectableMethod, object> extractResponse = null)
        {
            log?.LogTrace("calling {} method matching the url {}", callType, url);
            LocateMethod(request, methods, out InjectableMethod method, out object[] pars);
            if (method==null)
            {
                method = methods.First();
                if (!method.IsModelUpdateOrSave)
                    throw new CallNotFoundException("Unable to locate method with matching parameters");
            }
            if (!await method.HasValidAccess(request, model, url, model?.id))
                throw new InsecureAccessException();
            if (processLoadedModel != null)
                model = (T)processLoadedModel(model, request);
            log?.LogTrace("Invoking the {} method {}.{} for the url {}", callType, typeof(T).FullName, method.Name, url);
            if (method.IsSlow)
            {
                string newPath = registerSlowMethod(url, method, model, pars, request,log);
                if (newPath!= null)
                {
                    context.Response.ContentType = "text/json";
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync(Utility.JsonEncode(newPath, log));
                }
                else
                    throw new SlowMethodRegistrationFailed();
            }
            else
            {
                if (method.ReturnType == typeof(void))
                {
                    await method.InvokeAsync(model,request, pars: pars, responseHeaders:context.Response.Headers);
                    context.Response.ContentType= "text/json";
                    context.Response.StatusCode= 200;
                    await context.Response.WriteAsync("");
                }
                else if (method.ReturnType==typeof(string) && !method.IsArrayReturn)
                {
                    context.Response.StatusCode= 200;
                    string tmp = (string)(await method.InvokeAsync(model,request, pars: pars, responseHeaders: context.Response.Headers));
                    context.Response.ContentType= (tmp==null ? "text/json" : "text/text");
                    await context.Response.WriteAsync((tmp??Utility.JsonEncode(tmp, log)));
                }
                else
                {
                    context.Response.ContentType= "text/json";
                    context.Response.StatusCode= 200;
                    var resp = await method.InvokeAsync(model, request, pars: pars, responseHeaders: context.Response.Headers);
                    if (extractResponse!=null)
                        resp = extractResponse(model, resp,pars,method);
                    await context.Response.WriteAsync(Utility.JsonEncode(resp,log));
                }
            }
        }

        private static void LocateMethod(ModelRequestData request, IEnumerable<InjectableMethod> methods, out InjectableMethod method, out object[] pars)
        {
            method = null;
            pars = null;
            if (!request.Keys.Any())
                method = methods.FirstOrDefault(imi => imi.StrippedParameters.Length==0);
            else
            {
                foreach (InjectableMethod m in methods.Where(imi => imi.StrippedParameters.Count(p => !p.IsOut)==request.Keys.Count()))
                {
                    pars = new object[m.StrippedParameters.Length];
                    bool isMethod = true;
                    int index = 0;
                    foreach (ParameterInfo pi in m.StrippedParameters)
                    {
                        if (!pi.IsOut)
                        {
                            if (request.Keys.Contains(pi.Name, StringComparer.InvariantCultureIgnoreCase))
                            {
                                object val = null;
                                try
                                {
                                    val = request.GetValue(pi.ParameterType, pi.Name);
                                }
                                catch (InvalidCastException)
                                {
                                    isMethod=false;
                                    break;
                                }
                                if (val==null&&m.NotNullArguement!=null&&!m.NotNullArguement.IsParameterNullable(pi))
                                {
                                    isMethod=false;
                                    break;
                                }
                                pars[index] = val;
                            }
                            else
                            {
                                isMethod = false;
                                break;
                            }
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
    }
}
