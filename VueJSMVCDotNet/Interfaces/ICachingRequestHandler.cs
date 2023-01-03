using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.Interfaces
{
    internal interface ICachingRequestHandler : IRequestHandlerBase
    {
        bool HandlesRequest(string url, ModelRequestHandler.RequestMethods method, out object cachedItems);
        Task HandleRequest(string url, ModelRequestHandler.RequestMethods method, Hashtable formData, HttpContext context, ISecureSession session, IsValidCall securityCheck, object cachedItems);
    }
}
