using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Models.InvalidModels
{
    internal class ModelWithInvalidExposedMethods : IModel
    {
        public string id => null;
    }
}
