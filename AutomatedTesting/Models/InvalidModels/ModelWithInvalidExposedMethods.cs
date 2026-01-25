using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Models.InvalidModels
{
    internal class ModelWithInvalidEventStreamMethods : IModel
    {
        public string id => null;
    }
}
