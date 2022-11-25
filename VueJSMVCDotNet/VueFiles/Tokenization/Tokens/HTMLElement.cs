using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens.VueDirectives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VueFileParser.Tokenization.ParsedComponents.VueDirectives;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens
{
    internal class HTMLElement : IToken,ICompileable, IParsableComponent,IPatchable
    {
        private List<IToken> _children;
        private IToken[] _attributes => _children.Where(it => it is HTMLAttribute).ToArray();
        private IToken[] _directives => _children.Where(it => it is IVueDirective).ToArray();
        private IToken[] _eventDirectives => _directives.Where(it => it is EventDirective).ToArray();
        private IToken[] _withDirectives => _directives.Where(it => it is IWithVueDirective).ToArray();
        private IToken[] _content => _children.Where(it => !(it is HTMLAttribute || it is IVueDirective)).ToArray();
        private string _tag;
        public string Tag { get { return _tag; } }

        public HTMLElement(string tag)
        {
            _tag=tag;
            _children = new List<IToken>();
        }

        public void Add(HTMLAttribute attribute)
        {
            _children.Add(attribute);
        }

        public void Add(IVueDirective directive)
        {
            _children.Add(directive);
        }

        public void Add(IToken[] children)
        {
            _children.AddRange(children);
        }

        public string AsString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("<{0}", _tag);
                foreach (HTMLAttribute ha in _attributes)
                    sb.AppendFormat(" {0}", ha.AsString);
                foreach (IVueDirective directive in _directives)
                    sb.AppendFormat(" {0}", directive.AsString);
                if (_content.Length==0)
                    sb.Append("/>");
                else
                {
                    sb.AppendLine(">");
                    foreach (IToken child in _content)
                        sb.AppendLine(child.AsString);
                    sb.AppendFormat("</{0}>", _tag);
                }
                return sb.ToString();
            }
        }

        public int Cost
        {
            get
            {
                int ret = 0;
                foreach (IToken it in _children)
                {
                    if (it is IPatchable)
                        ret = Math.Max(ret, ((IPatchable)it).Cost);
                }
                return ret;
            }
        }

        private static readonly Regex _regVFor = new Regex("^\\{?([^,]+),?([^\\s]+)?\\}?\\sin\\s(.+)$", RegexOptions.Compiled|RegexOptions.ECMAScript);

        public void Compile(ref StringBuilder sb, IParsedComponent[] components,string name)
        {
            if (_withDirectives.Length>0)
                sb.Append("_withDirectives(_createElementVNode(");
            else
                sb.Append("_createElementBlock(");
            sb.AppendFormat("'{0}',\n", _tag);
            string bindValue = "";
            foreach (IVueDirective directive in _directives)
            {
                if (directive is FullBindDirective)
                    bindValue=((FullBindDirective)directive).Value;
            }
            sb.AppendLine((bindValue==null ? "{" : "_mergeProps({"));
            foreach (HTMLAttribute ha in _attributes)
            {
                ha.Compile(ref sb, components,name);
                sb.AppendLine(",");
            }
            foreach (IVueDirective directive in _directives)
            {
                if (directive is ICompileable)
                {
                    ((ICompileable)directive).Compile(ref sb, components, name);
                    sb.AppendLine(",");
                }
            }
            if (_directives.Length+_attributes.Length>0)
                sb.Length=sb.Length-3;
            if (bindValue!=null)
                sb.AppendFormat(@"
}},
{0},
{{
",VueFileCompiler.ProcessClassProperties(components,bindValue));
            int idx = 0;
            foreach (EventDirective ev in _eventDirectives)
            {
                ev.Compile(ref sb, components, name,idx);
                sb.AppendLine(",");
                idx++;
            }
            if (_eventDirectives.Length>0)
                sb.Length=sb.Length-3;
            sb.Append(@"
}");
            if (bindValue!=null)
                sb.Append(")");
            if (_content.Length>0)
            {
                sb.Append(",\n[");
                for (int x = 0; x<_content.Length; x++)
                {
                    bool add = true;
                    if (_content[x] is HTMLElement && ((HTMLElement)_content[x])._directives.Length>0)
                    {
                        foreach (IVueDirective vd in ((HTMLElement)_content[x])._directives)
                        {
                            if (vd is IfDirective)
                            {
                                add=false;
                                _ProcessIfDirective(ref x, ((IfDirective)vd).Value, ref sb, components, name);
                            }else if (vd is ForDirective)
                            {
                                add=false;
                                Match m = _regVFor.Match(((ForDirective)vd).Value);
                                sb.AppendFormat("(_openBlock(true, _createElementBlock(_Fragment,null,_renderList({0},({{1},{2}})=>{{ return (_openBlock(), ", new object[] { m.Groups[3].Value, (m.Groups[1].Value=="" ? "idx" : m.Groups[1].Value), m.Groups[2].Value });
                                if (_content[x] is ICompileable)
                                    ((ICompileable)_content[x]).Compile(ref sb, components, name);
                                else
                                    sb.Append(_content[x].AsString);
                                sb.AppendFormat(@");
}}), {0}))", (_content[x] is HTMLElement && ((HTMLElement)_content[x])._directives.Where(d=>d is KeyDirective).ToArray().Length>0 ? 1<<7 : 1<<8));

                            }
                        }
                    }
                    if (add)
                    {
                        if (_content[x] is ICompileable)
                            ((ICompileable)_content[x]).Compile(ref sb, components, name);
                        else
                            sb.Append(_content[x].AsString);
                    }
                    sb.Append(",");
                }
                sb.Length=sb.Length-1;
                sb.Append("]");
            }
            else
                sb.Append(",null");
            sb.AppendFormat(",{0})",Cost);
            if (_withDirectives.Length>0) {
                sb.AppendLine(",[");
                foreach (IWithVueDirective directive in _withDirectives)
                {
                    directive.Compile(ref sb, components, name);
                    sb.AppendLine(",");
                }
                sb.Length-=3;
                sb.AppendLine("])");
            }
        }

        private void _ProcessIfDirective(ref int x, string value, ref StringBuilder sb, IParsedComponent[] components,string name)
        {
            sb.AppendFormat("({0} ? ", value);
            int bracketCount = 1;
            bool hadElse = false;
            bool changed = true;
            if (_content[x] is ICompileable)
                ((ICompileable)_content[x]).Compile(ref sb, components,name);
            else
                sb.Append(_content[x].AsString);
            while (x+1<_content.Length && _content[x+1] is HTMLElement && ((HTMLElement)_content[x])._directives.Length>0 && changed)
            {
                changed=false;
                foreach (IVueDirective vd in ((HTMLElement)_content[x+1])._directives)
                {
                    if (vd is ElseDirective)
                    {
                        changed=true;
                        hadElse=true;
                        sb.Append(" : ");
                        if (_content[x] is ICompileable)
                            ((ICompileable)_content[x]).Compile(ref sb, components, name);
                        else
                            sb.Append(_content[x].AsString);
                        x++;
                        break;
                    }
                    else if (vd is ElseIfDirective)
                    {
                        changed=true;
                        sb.AppendFormat(" : ({0} ? ", ((ElseIfDirective)vd).Value);
                        if (_content[x] is ICompileable)
                            ((ICompileable)_content[x]).Compile(ref sb, components, name);
                        else
                            sb.Append(_content[x].AsString);
                        x++;
                        bracketCount++;
                    }
                }
            }
            if (!hadElse)
                sb.Append(" : ''");
            while (bracketCount>0)
            {
                sb.Append(")");
                bracketCount--;
            }
        }

        public IParsedComponent[] Parse()
        {
            List<IParsedComponent> ret = new List<IParsedComponent>(new IParsedComponent[] { new Import(new string[] { "createElementBlock as _createElementBlock" }, "vue") });
            if (_withDirectives.Length>0)
                ret.Add(new Import(new string[] { "withDirectives as _withDirectives", "createElementVNode as _createElementVNode" }, "vue"));
            foreach (IVueDirective directive in _directives)
            {
                if (directive is FullBindDirective)
                    ret.Add(new Import(new string[] { "mergeProps as _mergeProps" }, "vue"));
                else if (directive is ForDirective)
                    ret.Add(new Import(new string[] { "renderList as _renderList","Fragment as _Fragment" }, "vue"));
            }
            foreach (IToken it in _children)
            {
                if (it is IParsableComponent)
                {
                    IParsedComponent[] tmp = ((IParsableComponent)it).Parse();
                    if (tmp!=null)
                        ret.AddRange(tmp);
                }
            }
            return ret.ToArray();
        }
    }
}
