using AutomatedTesting.Models.InvalidModels;
using System.Threading.Tasks;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Handlers.InvalidModels
{
    internal class ModelWithNoRouteHandler : IModelHandler<ModelWithNoRoute>
    {
        ValueTask<ModelWithNoRoute> IModelHandler<ModelWithNoRoute>.LoadAsync(string id)
        => ValueTask.FromResult<ModelWithNoRoute>(null);
    }
}
