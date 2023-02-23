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

        [ModelListMethod()]
        public static Nullable<int> SearchNullable()
        {
            return null;
        }

        [ModelListMethod()]
        public static int[] SearchArray()
        {
            return null;
        }

        [ModelListMethod(paged:true)]
        public static List<ModelWithInvalidListMethods> InvalidPagedSignature()
        {
            return null;
        }

        [ModelListMethod()]
        public static List<ModelWithInvalidListMethods> WithOutParameter(out int par1)
        {
            par1=0;
            return null;
        }

        [ModelListMethod(paged:true)]
        public static List<ModelWithInvalidListMethods> PagedInvalidParameterType(decimal pageStartIndex, int pageSize, out int totalPages)
        {
            totalPages=0;
            return null;
        }

        [ModelListMethod(paged: true)]
        public static List<ModelWithInvalidListMethods> PagedInvalidOutParameter(out int totalPages,int pageStartIndex, int pageSize)
        {
            totalPages=0;
            return null;
        }
    }
}
