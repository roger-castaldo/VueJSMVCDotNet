using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.Interfaces
{
    internal interface IRequestHandler
    {
        void Init(List<Type> types);
        void ClearCache();
        bool HandlesRequest(string url, RequestHandler.RequestMethods method);
        Task HandleRequest(string url, RequestHandler.RequestMethods method, Hashtable formData, HttpContext context,ISecureSession session, IsValidCall securityCheck);
        #if NET
        void LoadTypes(List<Type> types);
        void UnloadTypes(List<Type> types);
        #endif
    }
}
