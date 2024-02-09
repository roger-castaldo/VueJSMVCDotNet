using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class InstanceMethodHandler : ModelRequestHandlerBase
    {
        private static readonly Regex regUrlSplit = new("^(.+)/([^/]+)/([^/]+)$", RegexOptions.Compiled|RegexOptions.ECMAScript, TimeSpan.FromMilliseconds(500));

        private readonly List<IModelActionHandler> handlers;

        public InstanceMethodHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase, ILogger log)
            :base(next,sessionFactory, registerSlowMethod, urlBase, log)
        {
            handlers=new List<IModelActionHandler>();
        }

        public override void ClearCache()
            => handlers.Clear();

        public override async Task ProcessRequest(HttpContext context)
        {
            string url = CleanURL(context);
            var match = regUrlSplit.Match(url);
            IModelActionHandler handler;
            if (ModelRequestHandlerBase.GetRequestMethod(context)==ModelRequestHandler.RequestMethods.METHOD 
                && match.Success 
                && (handler = handlers.FirstOrDefault(h => h.BaseURLs.Contains(match.Groups[1].Value, StringComparer.InvariantCultureIgnoreCase) && h.MethodNames.Contains(match.Groups[3].Value, StringComparer.InvariantCultureIgnoreCase)))!=null)
                await handler.Invoke(url, await ExtractParts(context), context, extractID: (url) =>
                    {
                        return regUrlSplit.Match(url).Groups[2].Value;
                    });
            else
                await next(context);
        }
        protected override void InternalLoadTypes(List<Type> types)
            => handlers.AddRange(
                    types.SelectMany(t=>
                        t.GetMethods(Constants.INSTANCE_METHOD_FLAGS)
                        .Where(m => m.GetCustomAttributes(typeof(ExposedMethod), false).Length>0)
                        .GroupBy(m => m.Name)
                        .Select(grp=> (IModelActionHandler)
                            typeof(ModelActionHandler<>).MakeGenericType(new Type[] { t })
                            .GetConstructor(new Type[] { typeof(MethodInfo[]), typeof(string), typeof(delRegisterSlowMethodInstance), typeof(ILogger) })
                            .Invoke(new object[] { grp.ToList(), "instanceMethod", registerSlowMethod, log })
                        )
                    )
                );
        
        protected override void InternalUnloadTypes(List<Type> types)
            => handlers.RemoveAll(h =>
                types.Contains(h.GetType().GetGenericArguments()[0])
            );

    }
}
