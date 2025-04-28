using AutomatedTesting.Models.InvalidModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Handlers.InvalidModels
{
    [ModelRouteAttribute("/models/ModelWithDuplicateMethods")]
    internal class ModelWithDuplicateMethodsHandler : IModelHandler<ModelWithDuplicateMethods>
    {
        ValueTask<ModelWithDuplicateMethods> IModelHandler<ModelWithDuplicateMethods>.LoadAsync(string id)
        => ValueTask.FromResult<ModelWithDuplicateMethods>(null);

        [ModelLoadAllMethodAttribute()]
        public List<ModelWithDuplicateMethods> LoadAll() { return null; }

        [ModelLoadAllMethodAttribute()]
        public List<ModelWithDuplicateMethods> DuplicateLoadAllMethod() { return null; }

        [ModelSaveMethodAttribute()]
        public string Save([ModelInstanceParameter] ModelWithDuplicateMethods model) { return string.Empty; }

        [ModelSaveMethodAttribute]
        public string DuplicateSaveMethod([ModelInstanceParameter] ModelWithDuplicateMethods model) { return string.Empty; }

        [ModelDeleteMethodAttribute()]
        public bool Delete([ModelIDParameter] string id) { return true; }

        [ModelDeleteMethodAttribute]
        public bool DuplicateDeleteMethod([ModelIDParameter] string id) { return true; }

        [ModelUpdateMethodAttribute()]
        public bool Update([ModelInstanceParameter] ModelWithDuplicateMethods model) { return true; }

        [ModelUpdateMethodAttribute]
        public bool DuplicateUpdateMethod([ModelInstanceParameter] ModelWithDuplicateMethods model) { return true; }
    }
}
