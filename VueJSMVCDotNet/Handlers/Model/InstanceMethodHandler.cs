using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Base;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.VueMiddleware;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class InstanceMethodHandler(ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase, ILogger log) 
        : ModelActionRequestHandler(sessionFactory,urlBase,log)
    {
        private static readonly Regex regUrlSplit = new("^(.+)/([^/]+)/([^/]+)$", RegexOptions.Compiled|RegexOptions.ECMAScript, TimeSpan.FromMilliseconds(500));

        protected override bool CanHandleRequest(HttpContext httpContext, out IModelActionHandler handler, out string cacheURL)
        {
            handler=null;
            cacheURL=null;
            string url = CleanURL(httpContext);
            var match = regUrlSplit.Match(url);
            if (GetRequestMethod(httpContext)==RequestMethods.METHOD
                && match.Success)
            {
                handler = FirstOrDefault(h=> h.BaseURLs.Contains(match.Groups[1].Value, StringComparer.InvariantCultureIgnoreCase) && h.MethodNames.Contains(match.Groups[3].Value, StringComparer.InvariantCultureIgnoreCase));
                cacheURL=url;
            }
            return handler!=null;
        }

        protected override async Task ExecuteActionHandlerAsync(HttpContext context, string url, IModelActionHandler handler)
            => await handler.Invoke(url, await ExtractParts(context), context, extractID: (url) =>
            {
                return regUrlSplit.Match(url).Groups[2].Value;
            });

        protected override IEnumerable<IModelActionHandler> GetHandlers(IEnumerable<Type> types)
            => types.SelectMany(t =>
                        t.GetMethods(Constants.INSTANCE_METHOD_FLAGS)
                        .Where(m => m.GetCustomAttributes(typeof(ExposedMethod), false).Length>0)
                        .GroupBy(m => m.Name)
                        .Select(grp => (IModelActionHandler)
                            typeof(ModelActionHandler<>).MakeGenericType(new Type[] { t })
                            .GetConstructor(new Type[] { typeof(MethodInfo[]), typeof(string), typeof(delRegisterSlowMethodInstance), typeof(ILogger) })
                            .Invoke(new object[] { grp.ToList(), "instanceMethod", registerSlowMethod, log })
                        )
                    );

    }
}
