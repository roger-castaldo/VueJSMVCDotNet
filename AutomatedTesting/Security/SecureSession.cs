using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutomatedTesting.Security
{
    internal class SecureSession : ISecureSession,ISecureSessionFactory
    {
        private string[] _rights = null;

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
                for(int x = 0; x<arrayList.Count; x++)
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

        public void LinkToRequest(HttpContext context)
        {
            context.Request.Headers.Add("RIGHTS", JSON.JsonEncode(_rights));
        }

        public ISecureSession ProduceFromContext(HttpContext context)
        {
            if (context.Request.Headers.ContainsKey("RIGHTS"))
                return new SecureSession((ArrayList)JSON.JsonDecode(context.Request.Headers["RIGHTS"].ToString()));
            else
                return new SecureSession();
        }
    }
}
