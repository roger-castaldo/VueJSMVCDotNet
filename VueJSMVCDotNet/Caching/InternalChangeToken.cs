using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VueJSMVCDotNet.Caching
{
    internal class InternalChangeToken : IChangeToken
    {
        private record ChangeCallback(Action<object> CallBack, object State, InternalChangeToken Container) : IDisposable
        {
            private bool disposedValue;

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: dispose managed state (managed objects)
                        Container.CallBacks.Remove(this);
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // TODO: set large fields to null
                    disposedValue=true;
                }
            }

            // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
            // ~ChangeCallback()
            // {
            //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            //     Dispose(disposing: false);
            // }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        private readonly List<ChangeCallback> CallBacks = [];
        private bool hasChanged = false;

        public bool ActiveChangeCallbacks => CallBacks.Any();

        public bool HasChanged {
            get => hasChanged;
            set
            {
                hasChanged=value;
                if (hasChanged && CallBacks.Count()>0)
                {
                    CallBacks.ForEach(cb => cb.CallBack(cb.State));
                    hasChanged=false;
                }
            }
        }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            var result = new ChangeCallback(callback, state, this);
            CallBacks.Add(result);
            return result;
        }
    }
}
