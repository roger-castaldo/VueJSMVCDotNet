using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelWithInvalidDataActionMethods.js")]
    [ModelRoute("/models/ModelWithInvalidDataActionMethods")]
    internal class ModelWithInvalidDataActionMethods : IModel
    {
        public string id => null;

        #region Loads
        [ModelLoadMethod()]
        public static ModelWithInvalidDataActionMethods NoArgumentLoad()
        {
            return null;
        }

        [ModelLoadMethod()]
        public static ModelWithInvalidDataActionMethods NotStringLoad(Guid id)
        {
            return null;
        }

        [ModelLoadMethod]
        public static void InvalidReturnLoad(string id)
        {

        }
        #endregion

        #region LoadAlls
        [ModelLoadAllMethod()]
        public static ModelWithInvalidDataActionMethods NotArrayReturnAll()
        {
            return null;
        }

        [ModelLoadAllMethod()]
        public static int[] WrongArrayTypeLoadAll()
        {
            return null;
        }

        [ModelLoadAllMethod()]
        public static List<int> WrongListTypeLoadAll()
        {
            return null;
        }

        [ModelLoadAllMethod()]
        public static List<ModelWithInvalidDataActionMethods> LoadAllWithInvalidArguements(string id)
        {
            return null;
        }
        #endregion

        public ModelWithInvalidDataActionMethods() { }


        [ModelSaveMethod()]
        public void InvalidSave() { }

        [ModelDeleteMethod()]
        public void InvalidDelete() { }

        [ModelUpdateMethod()]
        public void InvalidUpdate() {  }

    }
}
