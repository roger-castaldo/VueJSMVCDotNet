using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization
{
    internal class StyleSection : ITokenSection
    {
        private IToken[] _content;

        public string AsString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<style>");
                foreach (IToken child in _content)
                    sb.AppendLine(child.AsString);
                sb.AppendLine("</style>");
                return sb.ToString();
            }
        }

        public StyleSection(string content)
        {
            _content=new IToken[] {new TextToken(content)};
        }

        public void Compile(ref StringBuilder sb, IParsedComponent[] components,string name, ref int cacheCount)
        {
            sb.Append(_content[0].AsString);
        }

        public int CompareTo(object obj)
        {
            if (obj is ScriptSection)
                return 1;
            else
                return -1;
        }
    }
}
