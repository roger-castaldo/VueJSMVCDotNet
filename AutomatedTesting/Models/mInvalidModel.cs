using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Models
{
    [ModelRoute("/models/mInvalidModel")]
    [ModelJSFilePath("/resources/scripts/mInvalidModel.js")]
    internal class mInvalidModel : IModel
    {
        public string id
        {
            get { return null; }
        }
    }
}
