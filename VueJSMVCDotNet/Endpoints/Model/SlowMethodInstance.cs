using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.Interfaces.Internal;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal class SlowMethodInstance<H, M> : IDisposable
        where H : IModelHandler<M>
        where M : IModel
    {
        public record PullResponse(IEnumerable<object> Data, bool IsFinished, bool HasMore);

        private static readonly int TIMEOUT_MILLISECONDS = 60*1000;

        private readonly ConcurrentQueue<object> data;
        private bool finished;
        private bool completed;
        private Exception? error;
        private long lastCall;
        private bool disposedValue;
        private readonly Task execution;
        private readonly CancellationTokenSource token;
        private readonly ILogger? log;

        public SlowMethodInstance(InjectableMethod method, object?[] pars, IInternalRequestData? requestData, H instance, ILogger? log, M? modelInstance=default)
        {
            this.log=log;
            data=new ConcurrentQueue<object>();
            finished=false;
            completed=false;
            error=null;
            lastCall = Stopwatch.GetTimestamp();
            token = new();
            execution = new Task(async () =>
            {
                try
                {
                    if (method.ReturnType==typeof(void))
                        await method.InvokeAsync<object, M>(instance, requestData, log, pars: pars, addItem: new AddItem(AddItem), modelInstance: modelInstance);
                    else
                    {
                        var result = await method.InvokeAsync<object, M>(instance, requestData, log, pars: pars, addItem: new AddItem(AddItem), modelInstance: modelInstance);
                        AddItem(result, true);
                    }
                }
                catch (Exception e)
                {
                    log?.LogError(e, "Slow method execution error, {ErrorMessage}", e.Message);
                    error=e;
                }
            }, token.Token);
            execution.GetAwaiter().OnCompleted(new Action(() => { finished = true; }));
            execution.Start();
        }

        public void AddItem(object? item, bool isLast)
        {
            if (item!=null)
                data.Enqueue(item);
            finished=isLast;
        }

        public bool IsFinished
            => completed;

        public bool IsExpired
            => Stopwatch.GetElapsedTime(lastCall).TotalMilliseconds > TIMEOUT_MILLISECONDS;

        public async Task HandleRequest(HttpContext context)
        {
            if (error!=null)
            {
                log?.LogError(error, "Slow method request handling error, {ErrorMessage}", error.Message);
                context.Response.ContentType= "text/text";
                context.Response.StatusCode = 500;
                finished=true;
                completed=true;
                await context.Response.WriteAsync("Error");
            }
            else
            {
                lastCall = Stopwatch.GetTimestamp();
                List<object> ret = [];
                while (ret.Count<5&&!data.IsEmpty)
                {
                    if (data.TryDequeue(out var obj))
                        ret.Add(obj);
                    else
                        break;
                }
                context.Response.ContentType= "application/json";
                context.Response.StatusCode = 200;
                completed = finished&&data.IsEmpty;
                await context.Response.WriteAsync(Utility.JsonEncode(new PullResponse(ret, finished&&data.IsEmpty, !data.IsEmpty), await Helper.ExtractPartsAsync(context, log)));
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing && execution.Status==TaskStatus.Running)
                {
                    try
                    {
                        token.Cancel();
                        token.Dispose();
                    }
                    catch (Exception ex)
                    {
                        log?.LogError(ex, "Error disposing SlowMethodInstance, {ErrorMessage}", ex.Message);
                    }
                }
                disposedValue=true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
