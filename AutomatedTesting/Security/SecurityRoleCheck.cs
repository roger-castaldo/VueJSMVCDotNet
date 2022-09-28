using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting.Security
{
    internal class SecurityRoleCheck : ASecurityCheck
    {
        private string _right;
        
        public SecurityRoleCheck(string right)
        {
            _right = right;
        }

        public override bool HasValidAccess(ISecureSession session, IModel model, string url, Hashtable parameters)
        {
            return ((SecureSession)session).HasRight(_right);
        }
    }
}
