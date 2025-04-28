using AutomatedTesting.Models.InvalidModels;
using System.Threading.Tasks;
using VueJSMVCDotNet;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Handlers.InvalidModels
{
    [ModelRouteAttribute("/models/ModelWithInvalidExposedMethods")]
    internal class ModelWithInvalidExposedMethodsHandler : IModelHandler<ModelWithInvalidExposedMethods>
    {
        ValueTask<ModelWithInvalidExposedMethods> IModelHandler<ModelWithInvalidExposedMethods>.LoadAsync(string id)
        => ValueTask.FromResult<ModelWithInvalidExposedMethods>(null);

        [ExposedMethodAttribute()]
        public void DuplicateExposedStaticMethod(int par1) { }

        [ExposedMethodAttribute()]
        public void DuplicateExposedStaticMethod(string par1) { }

        [ExposedMethodAttribute]
        public void NotSlowWithAddItem(AddItem addItem) { }

        [ExposedMethodAttribute(isSlow: true)]
        public bool SlowWithAddItemAndReturn(AddItem addItem) { return true; }
    }
}
