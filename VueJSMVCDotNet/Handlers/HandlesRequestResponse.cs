using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Handlers
{
    internal record HandlesRequestResponse(bool Result,object State,string CacheURL, IRequestHandler RequestHandler) 
        : IHandlesRequestResponse;
}
