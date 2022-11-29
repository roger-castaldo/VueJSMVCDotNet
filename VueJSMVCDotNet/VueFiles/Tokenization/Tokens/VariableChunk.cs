using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens
{
    internal class VariableChunk : IToken, ICompileable,IParsableComponent
    {
        private string _value;

        public VariableChunk(string value)
        {
            _value = value.TrimStart('{').TrimEnd('}');
        }

        public string AsString => string.Format("{{{{{0}}}}}", _value);

        public void Compile(ref StringBuilder sb, IParsedComponent[] components,string name, ref int cacheCount)
        {
            sb.AppendFormat("_createTextVNode(_toDisplayString({0}),1)", VueFileCompiler.ProcessClassProperties(components,_value));
        }

        public IParsedComponent[] Parse()
        {
            return new IParsedComponent[]
            {
                new Import(new string[]{"toDisplayString as _toDisplayString","createTextVNode as _createTextVNode"},"vue")
            };
        }
    }
}
