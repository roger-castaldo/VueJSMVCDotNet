using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Threading;
using System.Threading.Channels;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal class EventStreamEndpoint<H, M>(ILogger? logger) :
        AModelEndpoint<H, M>(logger)
        where H : IModelHandler<M>
        where M : IModel
    {
        protected override IEnumerable<Endpoint> ProduceEndpoints(IEnumerable<ModelRouteAttribute> routes)
            => typeof(H).GetMethods(Constants.METHOD_FLAGS)
                .Where(m => m.GetCustomAttribute<EventStreamMethodAttribute>(false)!=null)
                .SelectMany(m =>
                {
                    var method = new InjectableMethod(m, ExtractSecurityChecks(m));
                    return routes.Select(mra =>
                        BuildEndpoint<H,M>(
                            async (context) =>
                            {
                                if (!await ValidateAccessAsync(context, Logger, null, method.SecurityChecks, false))
                                    await ReturnInsecure(context);
                                else
                                {
                                    var requestData = await Helper.ExtractPartsAsync(context, Logger);
                                    object?[] pars = new object?[method.StrippedParameters.Length];
                                    var writerCts = new CancellationTokenSource();
                                    var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, writerCts.Token);
                                    var channel = Channel.CreateUnbounded<object>();
                                    for (var x = 0; x<pars.Length; x++)
                                    {
                                        if (Equals(method.StrippedParameters[x].ParameterType, typeof(ChannelWriter<object>)))
                                            pars[x] = channel.Writer;
                                        else if (Equals(method.StrippedParameters[x].ParameterType, typeof(CancellationToken)))
                                            pars[x] = linkedCts.Token;
                                        else if (context.Request.Query.TryGetValue(method.StrippedParameters[x].Name!, out var value)) {
                                            try
                                            {
                                                pars[x] = EventStreamHelper.ConvertValue(method.StrippedParameters[x].ParameterType,value);
                                            }
                                            catch (Exception)
                                            {
                                                linkedCts.Dispose();
                                                writerCts.Dispose();
                                                await ReturnNotFound(context);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            linkedCts.Dispose();
                                            writerCts.Dispose();
                                            await ReturnNotFound(context);
                                            return;
                                        }
                                    }
                                    
                                    context.Response.Headers.Append("Content-Type", "text/event-stream");

                                    var task = method.InvokeAsync<object, M>(await CreateLoaderAsync(context), context, Logger, pars: pars);

                                    try
                                    {
                                        await foreach(var message in channel.Reader.ReadAllAsync(context.RequestAborted))
                                        {
                                            await context.Response.WriteAsync($"event: {EventStreamHelper.MessageEvent}\ndata: {Utility.JsonEncode(message,requestData)}\n\n");
                                            await context.Response.Body.FlushAsync();
                                        }

                                        await context.Response.WriteAsync($"event: {EventStreamHelper.CloseEvent}\ndata: complete\n\n");
                                        await context.Response.Body.FlushAsync();
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        writerCts.Cancel();
                                    }

                                    await task;
                                }
                            },
                            ProduceRoute(mra.Path, method.UsesModel, $"/{method.Name}"),
                            0,
                            $"Event Stream call for {typeof(H).Name}.{method.Name}",
                            [HttpMethods.Get],
                            m
                        )
                    );
                });
    }
}
