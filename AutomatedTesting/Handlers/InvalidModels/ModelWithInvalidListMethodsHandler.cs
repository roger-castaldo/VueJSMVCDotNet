using AutomatedTesting.Models.InvalidModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Handlers.InvalidModels
{
    [ModelRouteAttribute("/models/ModelWithInvalidListMethods")]
    internal class ModelWithInvalidListMethodsHandler : IModelHandler<ModelWithInvalidListMethods>
    {
        ValueTask<ModelWithInvalidListMethods> IModelHandler<ModelWithInvalidListMethods>.LoadAsync(string id)
        => ValueTask.FromResult<ModelWithInvalidListMethods>(null);

        [ModelListMethodAttribute()]
        public Nullable<int> SearchNullable()
        {
            return null;
        }

        [ModelListMethodAttribute()]
        public int[] SearchArray()
        {
            return null;
        }

        [ModelListMethodAttribute(paged: true)]
        public List<ModelWithInvalidListMethods> InvalidPagedSignature()
        {
            return null;
        }

        [ModelListMethodAttribute()]
        public List<ModelWithInvalidListMethods> WithOutParameter(out int par1)
        {
            par1=0;
            return null;
        }

        [ModelListMethodAttribute(paged: true)]
        public List<ModelWithInvalidListMethods> PagedInvalidParameterType(decimal pageStartIndex, int pageSize, out int totalPages)
        {
            totalPages=0;
            return null;
        }

        [ModelListMethodAttribute(paged: true)]
        public List<ModelWithInvalidListMethods> PagedInvalidOutParameter(out int totalPages, int pageStartIndex, int pageSize)
        {
            totalPages=0;
            return null;
        }
    }
}
