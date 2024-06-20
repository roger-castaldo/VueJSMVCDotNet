using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VueJSMVCDotNet.Interfaces
{
    internal interface INonCachingRequestHandler
    {
        Task ProduceResponseAsync(HttpContext context, object state);
    }
}
