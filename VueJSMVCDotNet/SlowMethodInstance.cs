using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Interfaces;
using System.Collections.Concurrent;
using System.Threading;

namespace VueJSMVCDotNet
{
    internal class SlowMethodInstance : IDisposable
    {
        public readonly struct SPullResponse
        {
            public IEnumerable<object> Data { get; init; }
            public bool IsFinished { get; init; }
            public bool HasMore { get; init; }
        }

        private static readonly int TIMEOUT_MILLISECONDS = 60*1000;

        private readonly ConcurrentQueue<object> data;
        private bool finished;
        private bool completed;
        private Exception error;
        private DateTime lastCall;
        private readonly Task execution;
        private readonly CancellationTokenSource token;
        private readonly ILogger log;

        public SlowMethodInstance(InjectableMethod method,object model, object[] pars, IRequestData requestData, ILogger log)
        {
            this.log=log;
            data=new ConcurrentQueue<object>();
            finished=false;
            completed=false;
            error=null;
            lastCall=DateTime.Now;
            token = new CancellationTokenSource();
            execution = new Task(() =>
            {
                try
                {
                    if (method.ReturnType==typeof(void))
                        method.Invoke(model, requestData, pars: pars, addItem: new AddItem(AddItem));
                    else
                        AddItem(method.Invoke(model, requestData, pars: pars, addItem: new AddItem(AddItem)), true);
                }
                catch (Exception e)
                {
                    log?.LogError("Slow method execution error, {}",e.Message);
                    error=e;
                }
            }, token.Token);
            execution.GetAwaiter().OnCompleted(new Action(() => { finished = true; }));
            execution.Start();
        }

        public void AddItem(object item, bool isLast)
        {
            if (item!=null)
                data.Enqueue(item);
            finished=isLast;
        }

        public bool IsFinished
            => completed;

        public bool IsExpired 
            => DateTime.Now.Subtract(lastCall).TotalMilliseconds > TIMEOUT_MILLISECONDS;

        public Task HandleRequest(HttpContext context)
        {
            if (error!=null)
            {
                log?.LogError("Slow method request handling error, {}",error.Message);
                context.Response.ContentType= "text/text";
                context.Response.StatusCode = 500;
                finished=true;
                completed=true;
                return context.Response.WriteAsync("Error");
            }
            else
            {
                lastCall = DateTime.Now;
                List<object> ret = new();
                while (ret.Count<5&&!data.IsEmpty)
                {
                    if (data.TryDequeue(out object obj))
                        ret.Add(obj);
                    else
                        break;
                }
                context.Response.ContentType= "text/json";
                context.Response.StatusCode = 200;
                completed = finished&&data.IsEmpty;
                return context.Response.WriteAsync(Utility.JsonEncode(new SPullResponse()
                {
                    Data=ret,
                    IsFinished=finished&&data.IsEmpty,
                    HasMore=!data.IsEmpty
                }, log));
            }
        }

        public void Dispose()
        {
            if (execution.Status==TaskStatus.Running)
            {
                try
                {
                    token.Cancel();
                }
                catch (Exception ex) { 
                    log?.LogError("Error disposing SlowMethodInstance, {}",ex.Message);
                }
            }
        }
    }
}
