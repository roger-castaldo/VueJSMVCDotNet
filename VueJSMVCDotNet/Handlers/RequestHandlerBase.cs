using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VueJSMVCDotNet.Handlers
{
    internal abstract class RequestHandlerBase : IDisposable
    {
        protected readonly RequestDelegate _next;

        public RequestHandlerBase(RequestDelegate next)
        {
            _next = next;
        }

        public abstract void Dispose();
        public abstract Task ProcessRequest(HttpContext context);
    }
}
