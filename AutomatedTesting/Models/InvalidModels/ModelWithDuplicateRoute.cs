using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelWithDuplicateMethods.js")]
    [ModelRouteAttribute("/models/ModelWithDuplicateMethods")]
    internal class ModelWithDuplicateRoute : IModel
    {
        public string id => null;

        [ModelLoadMethodAttribute()]
        public static ModelWithDuplicateRoute Load(string id)
        {
            return null;
        }
    }
}
