using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VueJSMVCDotNet.Interfaces
{
    internal interface IRequestHandler : IDisposable
    {
        IHandlesRequestResponse HandlesRequest(HttpContext context);
    }
}
