using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Caching
{
    internal record CachableResponse(string Content,string ContentType,DateTime Timestamp,IEnumerable<IChangeToken> ChangeTokens) 
        : ICachableResponse
    {
    }
}
