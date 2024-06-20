using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VueJSMVCDotNet.Interfaces
{
    internal interface ICachableResponse
    {
        string Content { get; }
        string ContentType { get; }
        DateTime Timestamp { get; }
        IEnumerable<IChangeToken> ChangeTokens { get; }
    }
}
