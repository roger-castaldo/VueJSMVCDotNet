using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces
{
    internal interface IWithVueDirective : IVueDirective,IParsableComponent
    {
        void ProduceDirective(ref StringBuilder sb, IParsedComponent[] components, string name,IHTMLElement owner);
    }
}
