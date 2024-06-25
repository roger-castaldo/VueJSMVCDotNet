using VueJSMVCDotNet;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelWithInvalidExposedMethods.js")]
    [ModelRouteAttribute("/models/ModelWithInvalidExposedMethods")]
    internal class ModelWithInvalidExposedMethods : IModel
    {
        public string id => null;

        [ModelLoadMethodAttribute()]
        public static ModelWithInvalidExposedMethods Load(string id)
        {
            return null;
        }

        [ExposedMethodAttribute()]
        public static void DuplicateExposedStaticMethod(int par1) { }

        [ExposedMethodAttribute()]
        public static void DuplicateExposedStaticMethod(string par1) { }

        [ExposedMethodAttribute]
        public static void NotSlowWithAddItem(AddItem addItem) { }

        [ExposedMethodAttribute(isSlow: true)]
        public static bool SlowWithAddItemAndReturn(AddItem addItem) { return true; }
    }
}
