using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VueJSMVCDotNet
{
    internal class SlowMethodInstance : IDisposable
    {
        public readonly struct SPullResponse
        {
            public object[] Data { get; init; }
            public bool IsFinished { get; init; }
            public bool HasMore { get; init; }
        }

        private static readonly int _TIMEOUT_MILLISECONDS = 60*1000;

        private readonly ConcurrentQueue<object> _data;
        private bool _finished;
        private bool _completed;
        private Exception _error;
        private DateTime _lastCall;
        private readonly Task _execution;
        private readonly CancellationTokenSource _token;
        private readonly ILogger log;

        public SlowMethodInstance(InjectableMethod method,object model, object[] pars, IRequestData requestData, ILogger log)
        {
            _data=new ConcurrentQueue<object>();
            _finished=false;
            _completed=false;
            _error=null;
            _lastCall=DateTime.Now;
            _token = new CancellationTokenSource();
            this.log=log;
            _execution = new Task(() =>
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
                    _error=e;
                }
            }, _token.Token);
            _execution.GetAwaiter().OnCompleted(new Action(() => { _finished = true; }));
            _execution.Start();
        }

        public void AddItem(object item, bool isLast)
        {
            if (item!=null)
                _data.Enqueue(item);
            _finished=isLast;
        }

        public bool IsFinished
        {
            get { return _completed; }
        }

        public bool IsExpired { get { return DateTime.Now.Subtract(_lastCall).TotalMilliseconds > _TIMEOUT_MILLISECONDS; } }

        public Task HandleRequest(HttpContext context)
        {
            if (_error!=null)
            {
                log?.LogError("Slow method request handling error, {}",_error.Message);
                context.Response.ContentType= "text/text";
                context.Response.StatusCode = 500;
                _finished=true;
                _completed=true;
                return context.Response.WriteAsync("Error");
            }
            else
            {
                _lastCall = DateTime.Now;
                List<object> ret = new();
                while (ret.Count<5&&!_data.IsEmpty)
                {
                    if (_data.TryDequeue(out object obj))
                        ret.Add(obj);
                    else
                        break;
                }
                context.Response.ContentType= "text/json";
                context.Response.StatusCode = 200;
                _completed = _finished&&_data.IsEmpty;
                return context.Response.WriteAsync(Utility.JsonEncode(new SPullResponse()
                {
                    Data=ret.ToArray(),
                    IsFinished=_finished&&_data.IsEmpty,
                    HasMore=!_data.IsEmpty
                }, log));
            }
        }

        public void Dispose()
        {
            if (_execution.Status==TaskStatus.Running)
            {
                try
                {
                    _token.Cancel();
                }
                catch (Exception ex) { 
                    log?.LogError("Error disposing SlowMethodInstance, {}",ex.Message);
                }
            }
        }
    }
}
