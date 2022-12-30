using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    internal class SlowMethodInstance : IDisposable
    {
        public struct sPullResponse
        {
            private object[] _data;
            public object[] Data { get { return _data; } }

            private bool _isFinished;
            public bool IsFinished { get { return _isFinished; } }

            private bool _hasMore;
            public bool HasMore{get{return _hasMore;} }

            public sPullResponse(object[] data,bool isFinished,bool hasMore)
            {
                _data=data;
                _isFinished=isFinished;
                _hasMore=hasMore;
            }
        }

        private static readonly int _TIMEOUT_MILLISECONDS = 5*60*1000;

        private ConcurrentQueue<object> _data;
        private bool _finished;
        private bool _completed;
        private Exception _error;
        private DateTime _lastCall;
        private Task _execution;
        private CancellationTokenSource _token;

        public SlowMethodInstance(MethodInfo method,object model,object[] pars,ISecureSession session)
        {
            _data=new ConcurrentQueue<object>();
            _finished=false;
            _completed=false;
            _error=null;
            _lastCall=DateTime.Now;
            _token = new CancellationTokenSource();
            _execution = new Task(() =>
            {
                try
                {
                    if (method.ReturnType==typeof(void))
                        Utility.InvokeMethod(method, model, pars: pars, session: session, addItem: new AddItem(AddItem));
                    else
                        AddItem(Utility.InvokeMethod(method, model, pars: pars, session: session, addItem: new AddItem(AddItem)), true);
                }catch(Exception e)
                {
                    Logger.LogError(e);
                    _error=e;
                }
            },_token.Token);
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
                Logger.LogError(_error);
                context.Response.ContentType= "text/text";
                context.Response.StatusCode = 500;
                _finished=true;
                _completed=true;
                return context.Response.WriteAsync("Error");
            }
            else
            {
                _lastCall = DateTime.Now;
                List<object> ret = new List<object>();
                while (ret.Count<5&&_data.Count>0)
                {
                    object obj;
                    if (_data.TryDequeue(out obj))
                        ret.Add(obj);
                    else
                        break;
                }
                context.Response.ContentType= "text/json";
                context.Response.StatusCode = 200;
                _completed = _finished&&_data.IsEmpty;
                return context.Response.WriteAsync(JSON.JsonEncode(new sPullResponse(ret.ToArray(), _finished&&_data.IsEmpty, !_data.IsEmpty)));
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
                catch (Exception ex) { }
            }
        }
    }
}
