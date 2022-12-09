using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens
{
    internal class TextToken : IToken,IPatchable,ICompileable,IParsableComponent
    {
        private string _value;
        public TextToken(string value)
        {
            _value=value;
        }

        public string AsString => _value;

        public int Cost => 1;

        public void Compile(ref StringBuilder sb, IParsedComponent[] components, string name, ref int cacheCount)
        {
            sb.AppendFormat("_createTextVNode(`{0}`,1)", _value);
        }

        public IParsedComponent[] Parse()
        {
            return new IParsedComponent[] { new Import(new string[] { "createTextVNode as _createTextVNode" }, Constants.VUE_IMPORT_NAME) };
        }
    }
}
