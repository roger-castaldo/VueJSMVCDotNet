using AutomatedTesting.Models.InvalidModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Handlers.InvalidModels
{
    [ModelRouteAttribute("/models/ModelWithInvalidDataActionMethods")]
    internal class ModelWithInvalidDataActionMethodsHandler : IModelHandler<ModelWithInvalidDataActionMethods>
    {
        ValueTask<ModelWithInvalidDataActionMethods> IModelHandler<ModelWithInvalidDataActionMethods>.LoadAsync(string id)
        => ValueTask.FromResult<ModelWithInvalidDataActionMethods>(null);

        #region LoadAlls
        [ModelLoadAllMethodAttribute()]
        public ModelWithInvalidDataActionMethods NotArrayReturnAll()
        {
            return null;
        }

        [ModelLoadAllMethodAttribute()]
        public int[] WrongArrayTypeLoadAll()
        {
            return null;
        }

        [ModelLoadAllMethodAttribute()]
        public List<int> WrongListTypeLoadAll()
        {
            return null;
        }

        [ModelLoadAllMethodAttribute()]
        public List<ModelWithInvalidDataActionMethods> LoadAllWithInvalidArguements(string id)
        {
            return null;
        }
        #endregion

        [ModelSaveMethodAttribute()]
        public void InvalidSave([ModelInstanceParameter] ModelWithInvalidDataActionMethods model) { }

        [ModelDeleteMethodAttribute()]
        public void InvalidDelete([ModelIDParameter] string id) { }

        [ModelUpdateMethodAttribute()]
        public void InvalidUpdate([ModelInstanceParameter] ModelWithInvalidDataActionMethods model) { }
    }
}
