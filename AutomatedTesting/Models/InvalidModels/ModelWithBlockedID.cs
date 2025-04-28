using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Models.InvalidModels
{
    internal class ModelWithBlockedID : IModel
    {
        [ModelIgnorePropertyAttribute]
        public string id => null;

    }
}
