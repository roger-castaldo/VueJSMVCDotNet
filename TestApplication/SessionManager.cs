using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Interfaces;

namespace TestApplication{
    internal class SessionManager : ISessionManager
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

        public Task<ISecureSession> ProduceFromContextAsync(HttpContext context)
        {
            return Task.FromResult<ISecureSession>(new SessionManager(context));
        }
    }
}