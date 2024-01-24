using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

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
