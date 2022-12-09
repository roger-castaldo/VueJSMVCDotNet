using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization
{
    internal class TemplateSection : ITokenSection, IParsableComponent, IComponentProperty, ICompileable
    {
        private IToken[] _content;
        public IToken[] Content => _content;

        public string AsString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<template>");
                foreach (IToken child in _content)
                    sb.AppendLine(child.AsString);
                sb.AppendLine("</template>");
                return sb.ToString();
            }
        }

        public TemplateSection(IToken[] content)
        {
            _content=content;
        }

        private bool _useSetup;

        public void Compile(bool useSetup,ref StringBuilder sb, IParsedComponent[] components, string name, ref int cacheCount)
        {
            _useSetup=useSetup;
            Compile(ref sb, components, name, ref cacheCount);
        }

        public void Compile(ref StringBuilder sb, IParsedComponent[] components,string name, ref int cacheCount)
        {
            if (_useSetup)
                sb.AppendLine(@"return (_ctx, _cache) => {");
            else
                sb.AppendLine(@"render:function(_ctx, _cache, $props, $setup, $data, $options) {");
sb.AppendLine("return (_openBlock(),");
            if (_content.Length>1)
            {
                sb.Append("[");
                foreach (IToken child in _content)
                {
                    if (child is ICompileable)
                        ((ICompileable)child).Compile(ref sb, components, name, ref cacheCount);
                    else if (child is IHTMLElement)
                        ((IHTMLElement)child).Compile(ref sb, components, name, ref cacheCount, _useSetup);
                    else
                        sb.AppendLine(child.AsString);
                }
                sb.Append("].map((content)=>{return content;})");
            }
            else
            {
                if (_content[0] is ICompileable)
                    ((ICompileable)_content[0]).Compile(ref sb, components, name,ref cacheCount);
                else if (_content[0] is IHTMLElement)
                    ((IHTMLElement)_content[0]).Compile(ref sb, components, name, ref cacheCount,_useSetup);
                else
                    sb.AppendLine(_content[0].AsString);
            }
            sb.Append(@");
}");
        }

        public IParsedComponent[] Parse()
        {
            List<IParsedComponent> ret = new List<IParsedComponent>(
                new IParsedComponent[]
                {
                    new Import(new string[]{ "openBlock as _openBlock"},Constants.VUE_IMPORT_NAME),
                    this
                }
            );
            foreach(IToken token in _content)
            {
                if (token is IParsableComponent)
                {
                    IParsedComponent[] tmp = ((IParsableComponent)token).Parse();
                    if (tmp!=null)
                        ret.AddRange(tmp);
                }
            }
            return ret.ToArray();
        }

        public int CompareTo(object obj)
        {
            return 1;
        }
    }
}
