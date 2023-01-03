using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.Interfaces
{
    internal interface IRequestHandlerBase
    {
        void Init(List<Type> types);
        void ClearCache();

        #if NET
        void LoadTypes(List<Type> types);
        void UnloadTypes(List<Type> types);
        #endif
    }
}
