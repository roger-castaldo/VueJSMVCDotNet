using System;
using Microsoft.AspNetCore.Http;

namespace TestApplication{
    internal class SessionManager : Org.Reddragonit.VueJSMVCDotNet.Interfaces.ISecureSession
    {
        [ThreadStatic()]
        private static HttpContext _current;
        public SessionManager(HttpContext context)
        {
            _current = context;
        }
    }
}