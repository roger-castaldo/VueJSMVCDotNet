using Org.Reddragonit.VueJSMVCDotNet.VueFiles;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VueFileParser.Tokenization.ParsedComponents.VueDirectives
{
    internal class ShowDirective : IWithVueDirective
    {
        private string _value;
        public string Value { get { return _value; } }
        public ShowDirective(string value)
        {
            _value=value;
        }

        public string AsString => string.Format("v-show=\"{0}\"", _value);

        public int Cost => 0;

        public void ProduceDirective(ref StringBuilder sb, IParsedComponent[] components, string name,HTMLElement owner)
        {
            sb.AppendFormat("[_vShow,{0}]", VueFileCompiler.ProcessClassProperties(components, _value));
        }

        public IParsedComponent[] Parse()
        {
            return new IParsedComponent[] { new Import(new string[] { "vShow as _vShow" }, "vue") };
        }
    }
}
