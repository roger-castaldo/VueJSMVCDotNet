using System;
using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;

namespace TestApplication{
    internal class SessionManager : ISessionManager, ISecureSessionFactory
    {
        [ThreadStatic()]
        private static HttpContext _current;
        public SessionManager(HttpContext context)
        {
            _current = context;
        }

        public SessionManager() { }

        public DateTime Start{
            get{
                if (_current.Session.GetString("Start")==null){
                    _current.Session.SetString("Start",DateTime.Now.ToString());
                }
                return DateTime.Parse(_current.Session.GetString("Start"));
            }
        }

        public ISecureSession ProduceFromContext(HttpContext context)
        {
            return new SessionManager(context);
        }
    }
}