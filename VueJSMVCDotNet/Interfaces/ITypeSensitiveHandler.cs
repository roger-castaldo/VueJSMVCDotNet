using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VueJSMVCDotNet.Interfaces
{
    internal interface ITypeSensitiveHandler : IRequestHandler
    {
        void ClearTypes();
        void LoadTypes(IEnumerable<Type> types);
        void UnloadTypes(IEnumerable<Type> types);
    }
}
