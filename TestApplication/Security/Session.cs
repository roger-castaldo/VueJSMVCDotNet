using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Interfaces;

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
