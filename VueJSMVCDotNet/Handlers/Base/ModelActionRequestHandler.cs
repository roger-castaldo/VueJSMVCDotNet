using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VueJSMVCDotNet.Handlers.Model;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Handlers.Base
{
    internal abstract class ModelActionRequestHandler(ISecureSessionFactory sessionFactory,string urlBase, ILogger log) : 
        ModelRequestHandlerBase(sessionFactory,urlBase,log), INonCachingRequestHandler
    {
        private readonly ReaderWriterLockSlim locker = new();
        private List<IModelActionHandler> handlers = [];

        protected abstract IEnumerable<IModelActionHandler> GetHandlers(IEnumerable<Type> types);
        protected abstract bool CanHandleRequest(HttpContext httpContext, out IModelActionHandler? handler, out string cacheURL);
        protected abstract Task ExecuteActionHandlerAsync(HttpContext context, string url, IModelActionHandler handler);
        protected virtual void RemoveHandlers(IEnumerable<Type> types, ref List<IModelActionHandler> handlers)
            => handlers.RemoveAll(h =>
                types.Contains(h.GetType().GetGenericArguments()[0])
            );
        public sealed override void LoadTypes(IEnumerable<Type> types)
        {
            locker.EnterWriteLock();
            handlers.AddRange(GetHandlers(types));
            locker.ExitWriteLock();
        }
        public sealed override void UnloadTypes(IEnumerable<Type> types)
        {
            locker.EnterWriteLock();
            RemoveHandlers(types, ref handlers);
            locker.ExitWriteLock();
        }
        public sealed override void ClearTypes()
        {
            locker.EnterWriteLock();
            handlers.Clear();
            locker.ExitWriteLock();
        }
        protected sealed override void InternalDispose()
        {
            locker.EnterWriteLock();
            handlers.Clear();
            locker.ExitWriteLock();
            locker.Dispose();
        }

        protected sealed override bool InternalHandlesRequest(HttpContext context, out object state, out string cacheURL)
        {
            var result = CanHandleRequest(context, out var handler, out cacheURL);
            state = new ModelRequestState(handler, cacheURL);
            return result;
        }

        public async Task ProduceResponseAsync(HttpContext context, object state)
        {
            var cachedState = (ModelRequestState)state;
            await ExecuteActionHandlerAsync(context, cachedState.URL, (IModelActionHandler)cachedState.State);
        }

        protected IEnumerable<IModelActionHandler> Where(Func<IModelActionHandler,bool> predicate)
        {
            locker.EnterReadLock();
            var result = handlers.Where(h=>predicate(h)).ToArray();
            locker.ExitReadLock();
            return result;
        }

        protected IModelActionHandler? FirstOrDefault(Func<IModelActionHandler,bool> predicate)
        {
            locker.EnterReadLock();
            var result = handlers.FirstOrDefault(h => predicate(h));
            locker.ExitReadLock();
            return result;
        }
    }
}
