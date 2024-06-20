using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VueJSMVCDotNet.Interfaces
{
    internal interface IHandlesRequestResponse
    {
        bool Result { get; }
        object State { get; }
        string CacheURL { get; }
        IRequestHandler RequestHandler { get; }
    }
}
