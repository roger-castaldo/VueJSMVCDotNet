using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelWithBlockedID.js")]
    [ModelRouteAttribute("/models/ModelWithBlockedID")]
    internal class ModelWithBlockedID : IModel
    {
        [ModelIgnorePropertyAttribute]
        public string id => null;

        [ModelLoadMethodAttribute()]
        public static ModelWithBlockedID Load(string id)
        {
            return null;
        }

    }
}
