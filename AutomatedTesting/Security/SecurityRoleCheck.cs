using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Security
{
    internal class SecurityRoleCheck : ASecurityCheck
    {
        private string _right;
        
        public SecurityRoleCheck(string right)
        {
            _right = right;
        }

        public override bool HasValidAccess(IRequestData data, IModel model, string url, string id)
        {
            return ((SecureSession)data.Session).HasRight(_right);
        }
    }
}
