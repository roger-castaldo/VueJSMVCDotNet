using System.Collections.Generic;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelWithDuplicateMethods.js")]
    [ModelRouteAttribute("/models/ModelWithDuplicateMethods")]
    internal class ModelWithDuplicateMethods : IModel
    {
        public string id => null;

        [ModelLoadMethodAttribute()]
        public static ModelWithDuplicateMethods Load(string id)
        {
            return null;
        }

        [ModelLoadMethodAttribute()]
        public static ModelWithDuplicateMethods DuplicateLoadMethod(string id)
        {
            return null;
        }

        [ModelLoadAllMethodAttribute()]
        public static List<ModelWithDuplicateMethods> LoadAll() { return null; }

        [ModelLoadAllMethodAttribute()]
        public static List<ModelWithDuplicateMethods> DuplicateLoadAllMethod() { return null; }

        public ModelWithDuplicateMethods() { }


        [ModelSaveMethodAttribute()]
        public bool Save() { return true; }

        [ModelSaveMethodAttribute]
        public bool DuplicateSaveMethod() { return true; }

        [ModelDeleteMethodAttribute()]
        public bool Delete() { return true; }

        [ModelDeleteMethodAttribute]
        public bool DuplicateDeleteMethod() { return true; }

        [ModelUpdateMethodAttribute()]
        public bool Update() { return true; }

        [ModelUpdateMethodAttribute]
        public bool DuplicateUpdateMethod() { return true; }
    }
}
