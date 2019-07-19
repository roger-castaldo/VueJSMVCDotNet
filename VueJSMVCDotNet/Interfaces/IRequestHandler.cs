using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Interfaces
{
    internal interface IRequestHandler
    {
        void Init(List<Type> types);
        void ClearCache();
        bool HandlesRequest(string url, RequestHandler.RequestMethods method);
        string HandleRequest(string url, RequestHandler.RequestMethods method, string formData, out string contentType, out int responseStatus);
    }
}
