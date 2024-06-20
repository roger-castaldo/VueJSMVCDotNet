using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VueJSMVCDotNet.Interfaces
{
    internal interface ICachingRequestHandler : IRequestHandler
    {
        Task<ICachableResponse> ProduceResponseAsync(HttpContext context, object state);
    }
}
