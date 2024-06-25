using System;
using System.Collections.Generic;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelWithInvalidDataActionMethods.js")]
    [ModelRouteAttribute("/models/ModelWithInvalidDataActionMethods")]
    internal class ModelWithInvalidDataActionMethods : IModel
    {
        public string id => null;

        #region Loads
        [ModelLoadMethodAttribute()]
        public static ModelWithInvalidDataActionMethods NoArgumentLoad()
        {
            return null;
        }

        [ModelLoadMethodAttribute()]
        public static ModelWithInvalidDataActionMethods NotStringLoad(Guid id)
        {
            return null;
        }

        [ModelLoadMethodAttribute]
        public static void InvalidReturnLoad(string id)
        {

        }
        #endregion

        #region LoadAlls
        [ModelLoadAllMethodAttribute()]
        public static ModelWithInvalidDataActionMethods NotArrayReturnAll()
        {
            return null;
        }

        [ModelLoadAllMethodAttribute()]
        public static int[] WrongArrayTypeLoadAll()
        {
            return null;
        }

        [ModelLoadAllMethodAttribute()]
        public static List<int> WrongListTypeLoadAll()
        {
            return null;
        }

        [ModelLoadAllMethodAttribute()]
        public static List<ModelWithInvalidDataActionMethods> LoadAllWithInvalidArguements(string id)
        {
            return null;
        }
        #endregion

        public ModelWithInvalidDataActionMethods() { }


        [ModelSaveMethodAttribute()]
        public void InvalidSave() { }

        [ModelDeleteMethodAttribute()]
        public void InvalidDelete() { }

        [ModelUpdateMethodAttribute()]
        public void InvalidUpdate() { }

    }
}
