using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
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

        public override bool HasValidAccess(IRequestData data, IModel model, string url,string id)
        {
            return data.Session!= null;
        }
    }
}
