using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces
{
    internal interface IEventDirective : IVueDirective
    {
        void ProduceEvent(ref StringBuilder sb, IParsedComponent[] components, string name, ref int cacheCount, HTMLElement owner);
    }
}
