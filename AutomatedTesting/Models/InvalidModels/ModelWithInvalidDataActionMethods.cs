using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Models.InvalidModels
{
    internal class ModelWithInvalidDataActionMethods : IModel
    {
        public string id => null;
        public ModelWithInvalidDataActionMethods() { }

    }
}
