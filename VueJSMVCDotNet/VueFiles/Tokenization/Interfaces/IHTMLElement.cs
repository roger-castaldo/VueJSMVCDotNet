using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces
{
    internal interface IHTMLElement : IToken, IParsableComponent, IPatchable
    {
        IVueDirective[] Directives { get; }

        string Tag { get; }
        string InputType { get; }

        void Add(IToken child);

        void Add(IToken[] children);

        void Compile(ref StringBuilder sb, IParsedComponent[] components, string name, ref int cacheCount,bool isSetup);
    }
}
