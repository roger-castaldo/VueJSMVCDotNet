using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelWithInvalidListMethods.js")]
    [ModelRoute("/models/ModelWithInvalidListMethods")]
    public class ModelWithInvalidListMethods : IModel
    {
        public string id => null;

        [ModelLoadMethod()]
        public static ModelWithInvalidListMethods Load(string id)
        {
            return null;
        }

        [ModelListMethod("/search/nullableResponse")]
        public static Nullable<int> SearchNullable()
        {
            return null;
        }

        [ModelListMethod("/search/arrayResponse")]
        public static int[] SearchArray()
        {
            return null;
        }

        [ModelListMethod("/search/paged",paged:true)]
        public static List<ModelWithInvalidListMethods> InvalidPagedSignature()
        {
            return null;
        }

        [ModelListMethod("/search/genericeTypeParameter")]
        public static List<ModelWithInvalidListMethods> WithGenericTypeParameter(Dictionary<string,string> par1)
        {
            return null;
        }

        [ModelListMethod("/search/arrayTypeParameter")]
        public static List<ModelWithInvalidListMethods> WithArrayTypeParameter(int[] par1)
        {
            return null;
        }

        [ModelListMethod("/search/outParameter")]
        public static List<ModelWithInvalidListMethods> WithOutParameter(out int par1)
        {
            par1=0;
            return null;
        }

        [ModelListMethod("/search/pagedInvalidType",paged:true)]
        public static List<ModelWithInvalidListMethods> PagedInvalidParameterType(decimal pageStartIndex, int pageSize, out int totalPages)
        {
            totalPages=0;
            return null;
        }
    }
}
