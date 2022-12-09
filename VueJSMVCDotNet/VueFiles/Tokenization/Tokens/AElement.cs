using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens.VueDirectives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens
{
    internal abstract class AElement : IHTMLElement
    {
        private List<IToken> _children;

        public List<IToken> Children { get { return _children; } }

        public AElement(string tag)
        {
            _children=new List<IToken>();
            _tag=tag;
        }

        public IVueDirective[] Directives => _children.Where(it => it is IVueDirective).Select(it=>(IVueDirective)it).ToArray();

        protected IToken[] Content => _children.Where(it => !(it is HTMLAttribute || it is IVueDirective)).ToArray();

        private string _tag;
        public string Tag => this._tag;
        public void Add(IToken child)
        {
            _children.Add(child);
        }

        public void Add(IToken[] children)
        {
            _children.AddRange(children);
        }

        public IParsedComponent[] Parse()
        {
            List<IParsedComponent> ret = new List<IParsedComponent>();
            ret.AddRange((ParsedComponents==null ? new IParsedComponent[] { } : ParsedComponents));
            foreach (IToken it in Children.Where(it => it is IParsableComponent))
            {
                IParsedComponent[] tmp = ((IParsableComponent)it).Parse();
                if (tmp!=null)
                    ret.AddRange(tmp);
            }
            return ret.ToArray();
        }

        public virtual string InputType => null;

        public virtual string AsString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("<{0}", _tag);
                foreach (HTMLAttribute ha in _children.Where(it => it is HTMLAttribute))
                    sb.AppendFormat(" {0}", ha.AsString);
                foreach (IVueDirective directive in Directives)
                    sb.AppendFormat(" {0}", directive.AsString);
                if (Content.Count()==0)
                    sb.Append("/>");
                else
                {
                    sb.AppendLine(">");
                    foreach (IToken child in Content)
                        sb.AppendLine(child.AsString);
                    sb.AppendFormat("</{0}>", _tag);
                }
                return sb.ToString();
            }
        }

        public abstract int Cost { get; }

        public abstract void Compile(ref StringBuilder sb, IParsedComponent[] components, string name, ref int cacheCount,bool isSetup);

        protected abstract IParsedComponent[] ParsedComponents { get; }
    }
}
