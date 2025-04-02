using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VueJSMVCDotNet.Interfaces.Internal
{
    internal interface IInternalRequestData : IRequestData
    {
        string? ModelID { get; }
        object? GetValue(Type t, string key);

        IModelHandler<M>? GetModelHandlerType<M>()
            where M : IModel;

        ValueTask<M?> LoadModelAsync<M>(string modelID)
            where M : IModel;

        ValueTask<object?> LoadModelAsync(Type modelType,string modelID);
    }
}
