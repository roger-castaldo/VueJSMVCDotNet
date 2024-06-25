using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelMissingEmptyConstructorForSave.js")]
    [ModelRouteAttribute("/models/ModelMissingEmptyConstructorForSave")]
    internal class ModelMissingEmptyConstructorForSave : IModel
    {

        public string id => null;

        [ModelLoadMethodAttribute()]
        public static ModelMissingEmptyConstructorForSave Load(string id)
        {
            return null;
        }

        public ModelMissingEmptyConstructorForSave(bool noEmpty) { }

        [ModelSaveMethodAttribute()]
        public bool Save() { return true; }
    }
}
