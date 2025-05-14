using System.Threading.Tasks;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Security
{
    internal class SecurityRoleCheck : ASecurityCheckAttribute
    {
        private string _right;

        public SecurityRoleCheck(string right)
        {
            _right = right;
        }

        public override Task<bool> HasValidAccessAsync(IRequestData data, IModel model, string url, string id)
        {
            return Task.FromResult<bool>(((SecureSession)data.Session).HasRight(_right));
        }
    }
}
