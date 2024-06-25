using System;
using System.Collections.Generic;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelWithInvalidListMethods.js")]
    [ModelRouteAttribute("/models/ModelWithInvalidListMethods")]
    public class ModelWithInvalidListMethods : IModel
    {
        public string id => null;

        [ModelLoadMethodAttribute()]
        public static ModelWithInvalidListMethods Load(string id)
        {
            return null;
        }

        [ModelListMethodAttribute()]
        public static Nullable<int> SearchNullable()
        {
            return null;
        }

        [ModelListMethodAttribute()]
        public static int[] SearchArray()
        {
            return null;
        }

        [ModelListMethodAttribute(paged: true)]
        public static List<ModelWithInvalidListMethods> InvalidPagedSignature()
        {
            return null;
        }

        [ModelListMethodAttribute()]
        public static List<ModelWithInvalidListMethods> WithOutParameter(out int par1)
        {
            par1=0;
            return null;
        }

        [ModelListMethodAttribute(paged: true)]
        public static List<ModelWithInvalidListMethods> PagedInvalidParameterType(decimal pageStartIndex, int pageSize, out int totalPages)
        {
            totalPages=0;
            return null;
        }

        [ModelListMethodAttribute(paged: true)]
        public static List<ModelWithInvalidListMethods> PagedInvalidOutParameter(out int totalPages, int pageStartIndex, int pageSize)
        {
            totalPages=0;
            return null;
        }
    }
}
