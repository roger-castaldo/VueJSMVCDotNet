using AutomatedTesting.Models.InvalidModels;
using System.Threading.Tasks;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Handlers.InvalidModels
{
    [ModelRouteAttribute("/models/ModelWithDuplicateMethods")]
    internal class ModelWithDuplicateRouteHandler : IModelHandler<ModelWithDuplicateRoute>
    {
        ValueTask<ModelWithDuplicateRoute> IModelHandler<ModelWithDuplicateRoute>.LoadAsync(string id)
        => ValueTask.FromResult<ModelWithDuplicateRoute>(null);
    }
}
