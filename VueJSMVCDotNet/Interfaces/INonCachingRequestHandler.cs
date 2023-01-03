using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.Interfaces
{
    internal interface INonCachingRequestHandler : IRequestHandlerBase
    {
        bool HandlesRequest(string url, ModelRequestHandler.RequestMethods method);
        Task HandleRequest(string url, ModelRequestHandler.RequestMethods method, Hashtable formData, HttpContext context,ISecureSession session, IsValidCall securityCheck);
        
    }
}
