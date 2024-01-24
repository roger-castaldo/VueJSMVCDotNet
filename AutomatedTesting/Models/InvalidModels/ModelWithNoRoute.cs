using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelWithNoRoute.js")]
    internal class ModelWithNoRoute : IModel
    {
        public string id => null;

        [ModelLoadMethod()]
        public static ModelWithNoRoute Load(string id)
        {
            return null;
        }
    }
}
