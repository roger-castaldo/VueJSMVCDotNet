using System.Threading.Tasks;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace TestApplication.Security
{
    public class IsLoggedIn : ASecurityCheck
    {
        public IsLoggedIn()
        {
        }

        public override Task<bool> HasValidAccessAsync(IRequestData data, IModel model, string url,string id)
        {
            return Task.FromResult<bool>(data.Session!= null);
        }
    }
}
