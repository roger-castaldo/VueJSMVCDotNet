using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Handlers.Model;

namespace VueJSMVCDotNet.Interfaces
{
    internal interface IModelActionHandler
    {
        IEnumerable<string> BaseURLs { get; }
        IEnumerable<string> MethodNames { get; }
        Task Invoke(string url, ModelRequestData request, HttpContext context, Func<string, string> extractID = null, Func<IModel, ModelRequestData, IModel> processLoadedModel = null);
        Task InvokeWithoutLoad(string url, ModelRequestData request, HttpContext context, IModel model=null, Func<IModel, object, object[],InjectableMethod, object> extractResponse = null);

        Task<IModel> Load(string url, ModelRequestData request, Func<string, string> extractID = null);
    }
}
