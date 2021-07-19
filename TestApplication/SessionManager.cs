using System;
using Microsoft.AspNetCore.Http;

namespace TestApplication{
    internal class SessionManager : ISessionManager
    {
        [ThreadStatic()]
        private static HttpContext _current;
        public SessionManager(HttpContext context)
        {
            _current = context;
        }

        public DateTime Start{
            get{
                if (_current.Session.GetString("Start")==null){
                    _current.Session.SetString("Start",DateTime.Now.ToString());
                }
                return DateTime.Parse(_current.Session.GetString("Start"));
            }
        }
    }
}