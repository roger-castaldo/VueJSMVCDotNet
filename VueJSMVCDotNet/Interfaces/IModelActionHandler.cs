using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Handlers.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.ModelRequestHandlerBase;

namespace Org.Reddragonit.VueJSMVCDotNet.Interfaces
{
    internal interface IModelActionHandler
    {
        IEnumerable<string> BaseURLs { get; }
        IEnumerable<string> MethodNames { get; }
        Task Invoke(string url, ModelRequestData request, HttpContext context, Func<string, string> extractID = null, Func<IModel, ModelRequestData, IModel> processLoadedModel = null);
        Task InvokeWithoutLoad(string url, ModelRequestData request, HttpContext context, IModel model=null, Func<IModel, object, object[],InjectableMethod, object> extractResponse = null);

        IModel Load(string url, ModelRequestData request, Func<string, string> extractID = null);
    }
}
