using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting.FileProvider
{
    internal class EmbeddedChangeToken : IChangeToken
    {
        private struct sCallBack : IDisposable
        {
            private Action<object> _callback;
            private object _state;
            private EmbeddedChangeToken _container;

            public sCallBack(Action<object> callback, object state,EmbeddedChangeToken container)
            {
                _callback=callback;
                _state=state;
                _container=container;
            }

            public void Dispose()
            {
                _container._callbacks.Remove(this);
            }

            public void runIt()
            {
                _callback.Invoke(_state);
            }

            
        }

        private bool _changed;
        private List<sCallBack> _callbacks;

        public EmbeddedChangeToken()
        {
            _changed=false;
            _callbacks=new List<sCallBack>();
        }

        public bool HasChanged => _changed;

        public bool ActiveChangeCallbacks => _callbacks.Count>0;

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            sCallBack ret = new  sCallBack(callback, state,this);
            _callbacks.Add(ret);
            return ret;
        }

        internal void Trigger()
        {
            _changed=true;
            foreach (sCallBack scb in _callbacks)
                scb.runIt();
        }
    }
}
