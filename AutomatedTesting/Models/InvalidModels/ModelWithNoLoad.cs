using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelWithNoLoad.js")]
    [ModelRouteAttribute("/models/ModelWithNoLoad")]
    internal class ModelWithNoLoad : IModel
    {
        public string id => null;

    }
}
