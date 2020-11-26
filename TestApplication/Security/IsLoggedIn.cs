using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestApplication.Security
{
    public class IsLoggedIn : ASecurityCheck
    {
        public IsLoggedIn()
        {
        }

        public override bool HasValidAccess(ISecureSession session, IModel model, string url, Hashtable parameters)
        {
            return session != null;
        }
    }
}
