using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Security
{
    internal class SecureSession : ISecureSession, ISecureSessionFactory
    {
        private readonly string[] _rights = null;

        public SecureSession()
        {
        }

        public SecureSession(string[] rights)
        {
            _rights = rights;
        }

        public SecureSession(ArrayList arrayList)
        {
            if (arrayList!=null)
            {
                _rights = new string[arrayList.Count];
                for (int x = 0; x<arrayList.Count; x++)
                {
                    _rights[x] = (string)arrayList[x];
                }
            }
        }

        public bool HasRight(string right)
        {
            if (_rights == null)
                return true;
            return _rights.Contains(right);
        }

        public Task<ISecureSession> ProduceFromContextAsync(HttpContext context)
            => Task.FromResult<ISecureSession>(this);
    }
}
