using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestApplication.Security
{
    public class Session : ISecureSession
    {
        private HttpContext _context;

        public Session(HttpContext context)
        {
            _context = context;
        }
    }
}
