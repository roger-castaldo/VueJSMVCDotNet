using AutomatedTesting.Models.InvalidModels;
using System.Threading.Tasks;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Handlers.InvalidModels
{
    [ModelRouteAttribute("/models/ModelWithBlockedID")]
    internal class ModelWithBlockedIDHandler : IModelHandler<ModelWithBlockedID>
    {
        ValueTask<ModelWithBlockedID> IModelHandler<ModelWithBlockedID>.LoadAsync(string id)
        => ValueTask.FromResult<ModelWithBlockedID>(null);
    }
}
