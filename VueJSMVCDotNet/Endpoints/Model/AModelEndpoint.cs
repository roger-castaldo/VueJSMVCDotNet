using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.Interfaces.Internal;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal abstract class AModelEndpoint<H, M> : AEndpoint, IEndpointHandler
        where H : IModelHandler<M>
        where M : IModel
    {
        protected readonly ASecurityCheckAttribute[] LoadSecurityChecks;

        protected AModelEndpoint(ILogger? logger)
            : base(logger)
        {
            var map = typeof(H).GetInterfaceMap(typeof(IModelHandler<M>));
            LoadSecurityChecks = ExtractSecurityChecks(
                        map.TargetMethods[Array.FindIndex(map.InterfaceMethods, m => Equals(m.Name, nameof(IModelHandler<M>.LoadAsync)))]
                    );
        }

        protected async Task<H> CreateLoaderAsync(HttpContext context)
        {
            var data = await Helper.ExtractPartsAsync(context, Logger);
            try
            {
                return ActivatorUtilities.CreateInstance<H>(context.RequestServices, data.Session!);
            }
            catch
            {
                return ActivatorUtilities.CreateInstance<H>(context.RequestServices);
            }
        }

        protected abstract IEnumerable<Endpoint> ProduceEndpoints(IEnumerable<ModelRouteAttribute> routes);

        IEnumerable<Endpoint> IEndpointHandler.AsEndpoints
            => ProduceEndpoints(typeof(H)
                .GetCustomAttributes<ModelRouteAttribute>()
            );

        protected ASecurityCheckAttribute[] ExtractSecurityChecks(MethodInfo method)
            => [.. typeof(H).GetCustomAttributes<ASecurityCheckAttribute>()
                .Concat(method.GetCustomAttributes<ASecurityCheckAttribute>())];

        protected static string GetModelID(HttpContext context)
            => context.Request.RouteValues[Helper.ID_PARAMETER_NAME]?.ToString()??throw new NullModelIdException();

        protected static RoutePattern ProduceRoute(string baseURL, bool includeID, string additional = "")
        {
            var result = new StringBuilder(baseURL);
            if (includeID)
                result.Append($"/{{{Helper.ID_PARAMETER_NAME}}}");
            if (!string.IsNullOrWhiteSpace(additional))
                result.Append(additional);
            return RoutePatternFactory.Parse(result.ToString());
        }

        protected async static ValueTask<bool> ValidateAccessAsync(HttpContext context, ILogger? logger, IModel? model, ASecurityCheckAttribute[] securityChecks, bool loadID = true)
        {
            var id = (loadID ? GetModelID(context) : model?.id);
            var data = await Helper.ExtractPartsAsync(context, logger);
            var url = context.Request.Path.ToString();
            foreach (var sc in securityChecks)
            {
                if (!await sc.HasValidAccessAsync(data, model, url, id))
                    return false;
            }
            return true;
        }

        protected static async Task<(InjectableMethod method, object?[] pars)?> LocateMethodAsync(HttpContext context, IEnumerable<InjectableMethod> methods, ILogger? logger)
        {
            var request = await Helper.ExtractPartsAsync(context, logger);
            InjectableMethod? method = null;
            object?[] pars = [];
            if (!request.Keys.Any())
                method = methods.FirstOrDefault(imi => imi.StrippedParameters.Length==0 && (
                    (string.IsNullOrWhiteSpace(request.ModelID) && !imi.RequiresModel)
                    || (!string.IsNullOrWhiteSpace(request.ModelID) && imi.RequiresModel)
                ));
            else
            {
                foreach (InjectableMethod m in methods.Where(imi => imi.StrippedParameters.Count(p => !p.IsOut)==request.Keys.Count()))
                {
                    pars = new object?[m.StrippedParameters.Length];
                    bool isMethod = true;
                    int index = 0;
                    foreach (ParameterInfo pi in m.StrippedParameters)
                    {
                        (isMethod, pars) = ExtractRequestParameter(m, pi, request, pars, index);
                        if (!isMethod)
                            break;
                        index++;
                    }
                    if (isMethod)
                    {
                        method = m;
                    }
                }
            }
            return (method!=null ? (method!, pars) : null);
        }

        private static (bool isMethod, object?[] pars) ExtractRequestParameter(InjectableMethod method, ParameterInfo pi, IInternalRequestData request, object?[] pars, int index)
        {
            if (!pi.IsOut)
            {
                if (request.Keys.Contains(pi.Name, StringComparer.InvariantCultureIgnoreCase))
                {
                    object? val;
                    try
                    {
                        val = request.GetValue(pi.ParameterType, pi.Name!);
                    }
                    catch (InvalidCastException)
                    {
                        return (false, pars);
                    }
                    if (val==null&&method.NotNullArguement!=null&&!method.NotNullArguement.IsParameterNullable(pi))
                        return (false, pars);
                    pars[index] = val;
                }
                else
                    return (false, pars);
            }
            return (true, pars);
        }
    }
}
