using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutomatedTesting.Security
{
    internal class SecureSession : ISecureSession
    {
        private string[] _rights = null;

        public SecureSession()
        {
        }

        public SecureSession(string[] rights)
        {
            _rights = rights;
        }

        public bool HasRight(string right)
        {
            if (_rights == null)
                return true;
            return _rights.Contains(right);
        }
    }
}
