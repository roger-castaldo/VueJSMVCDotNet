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
    internal class HTMLElement : AElement
    {
        private IToken[] _attributes => Children.Where(it => it is HTMLAttribute).ToArray();
        private IToken[] _eventDirectives => Directives.Where(it => it is IEventDirective).ToArray();
        private IToken[] _withDirectives => Directives.Where(it => it is IWithVueDirective).ToArray();

        public override string InputType
        {
            get
            {
                foreach (HTMLAttribute att in _attributes)
                {
                    if (att.Name=="type")
                        return att.Value;
                }
                foreach (IVueDirective directive in Directives)
                {
                    if (directive is BindDirective)
                    {
                        BindDirective bd = (BindDirective)directive;
                        if (bd.Command=="type")
                            return bd.Value;
                    }
                }
                return "unknown";
            }
        }

        public HTMLElement(string tag)
            : base(tag)
        { }

        public override int Cost
        {
            get
            {
                int ret = 0;
                foreach (IToken it in Children.Where(it=>it is IPatchable))
                {
                    ret = Math.Max(ret, ((IPatchable)it).Cost);
                }
                return ret;
            }
        }

        private static readonly Regex _regVFor = new Regex("^\\{?([^,]+),?([^\\s]+)?\\}?\\sin\\s(.+)$", RegexOptions.Compiled|RegexOptions.ECMAScript);

        public override void Compile(ref StringBuilder sb, IParsedComponent[] components,string name, ref int cacheCount,bool isSetup)
        {
            if (_withDirectives.Length>0)
                sb.Append("_withDirectives(_createElementVNode(");
            else
                sb.Append("_createElementBlock(");
            sb.AppendFormat("'{0}',\n", Tag);
            string bindValue = null;
            foreach (IVueDirective directive in Directives)
            {
                if (directive is FullBindDirective)
                    bindValue=((FullBindDirective)directive).Value;
            }
            sb.AppendLine((bindValue==null ? "{" : "_mergeProps({"));
            foreach (HTMLAttribute ha in _attributes)
            {
                ha.Compile(ref sb, components,name,ref cacheCount);
                sb.AppendLine(",");
            }
            foreach (IVueDirective directive in Directives)
            {
                if (directive is ICompileable)
                {
                    ((ICompileable)directive).Compile(ref sb, components, name,ref cacheCount);
                    sb.AppendLine(",");
                }
            }
            if (Directives.Count(t=>t is IComparable)+_attributes.Length>0)
                sb.Length=sb.Length-3;
            if (bindValue!=null)
                sb.AppendFormat(@"
}},
{0},
{{
",VueFileCompiler.ProcessClassProperties(components,bindValue));
            foreach (IEventDirective ev in _eventDirectives)
            {
                ev.ProduceEvent(ref sb, components, name,ref cacheCount,this,isSetup);
                sb.AppendLine(",");
            }
            if (_eventDirectives.Length>0)
                sb.Length=sb.Length-3;
            sb.Append(@"
}");
            if (bindValue!=null)
                sb.Append(")");
            if (Content.Length>0)
            {
                sb.Append(",\n[");
                for (int x = 0; x<Content.Length; x++)
                {
                    bool add = true;
                    if (Content[x] is IHTMLElement && ((IHTMLElement)Content[x]).Directives.Length>0)
                    {
                        foreach (IVueDirective vd in ((IHTMLElement)Content[x]).Directives)
                        {
                            if (vd is IfDirective)
                            {
                                add=false;
                                _ProcessIfDirective(ref x, ((IfDirective)vd).Value, ref sb, components, name,ref cacheCount,isSetup);
                            }else if (vd is ForDirective)
                            {
                                add=false;
                                Match m = _regVFor.Match(((ForDirective)vd).Value);
                                sb.AppendFormat("(_openBlock(true, _createElementBlock(_Fragment,null,_renderList({0},({{1},{2}})=>{{ return (_openBlock(), ", new object[] { m.Groups[3].Value, (m.Groups[1].Value=="" ? "idx" : m.Groups[1].Value), m.Groups[2].Value });
                                if (Content[x] is ICompileable)
                                    ((ICompileable)Content[x]).Compile(ref sb, components, name,ref cacheCount);
                                else if (Content[x] is IHTMLElement)
                                    ((IHTMLElement)Content[x]).Compile(ref sb, components, name, ref cacheCount, isSetup);
                                else
                                    sb.Append(Content[x].AsString);
                                sb.AppendFormat(@");
}}), {0}))", (Content[x] is IHTMLElement && ((IHTMLElement)Content[x]).Directives.Where(d=>d is KeyDirective).ToArray().Length>0 ? 1<<7 : 1<<8));

                            }
                        }
                    }
                    if (add)
                    {
                        if (Content[x] is ICompileable)
                            ((ICompileable)Content[x]).Compile(ref sb, components, name, ref cacheCount);
                        else if (Content[x] is IHTMLElement)
                            ((IHTMLElement)Content[x]).Compile(ref sb, components, name, ref cacheCount, isSetup);
                        else
                            sb.Append(Content[x].AsString);
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
                    directive.ProduceDirective(ref sb, components, name,this);
                    sb.AppendLine(",");
                }
                sb.Length-=3;
                sb.AppendLine("])");
            }
        }

        private void _ProcessIfDirective(ref int x, string value, ref StringBuilder sb, IParsedComponent[] components,string name,ref int cacheCount,bool isSetup)
        {
            sb.AppendFormat("({0} ? ", value);
            int bracketCount = 1;
            bool hadElse = false;
            bool changed = true;
            if (Content[x] is ICompileable)
                ((ICompileable)Content[x]).Compile(ref sb, components,name,ref cacheCount);
            else if (Content[x] is IHTMLElement)
                ((IHTMLElement)Content[x]).Compile(ref sb, components, name, ref cacheCount, isSetup);
            else
                sb.Append(Content[x].AsString);
            while (x+1<Content.Length && Content[x+1] is IHTMLElement && ((IHTMLElement)Content[x]).Directives.Length>0 && changed)
            {
                changed=false;
                foreach (IVueDirective vd in ((IHTMLElement)Content[x+1]).Directives)
                {
                    if (vd is ElseDirective)
                    {
                        changed=true;
                        hadElse=true;
                        sb.Append(" : ");
                        if (Content[x] is ICompileable)
                            ((ICompileable)Content[x]).Compile(ref sb, components, name,ref cacheCount);
                        else if (Content[x] is IHTMLElement)
                            ((IHTMLElement)Content[x]).Compile(ref sb, components, name, ref cacheCount, isSetup);
                        else
                            sb.Append(Content[x].AsString);
                        x++;
                        break;
                    }
                    else if (vd is ElseIfDirective)
                    {
                        changed=true;
                        sb.AppendFormat(" : ({0} ? ", ((ElseIfDirective)vd).Value);
                        if (Content[x] is ICompileable)
                            ((ICompileable)Content[x]).Compile(ref sb, components, name,ref cacheCount);
                        else if (Content[x] is IHTMLElement)
                            ((IHTMLElement)Content[x]).Compile(ref sb, components, name, ref cacheCount, isSetup);
                        else
                            sb.Append(Content[x].AsString);
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

        protected override IParsedComponent[] ParsedComponents
        {
            get
            {
                List<IParsedComponent> ret = new List<IParsedComponent>(new IParsedComponent[] { new Import(new string[] { "createElementBlock as _createElementBlock" }, Constants.VUE_IMPORT_NAME) });
                if (_withDirectives.Length>0)
                    ret.Add(new Import(new string[] { "withDirectives as _withDirectives", "createElementVNode as _createElementVNode" }, Constants.VUE_IMPORT_NAME));
                if (Directives.Count(di=>di is FullBindDirective)>0)
                    ret.Add(new Import(new string[] { "mergeProps as _mergeProps" }, Constants.VUE_IMPORT_NAME));
                if (Directives.Count(di=>di is ForDirective)>0)
                    ret.Add(new Import(new string[] { "renderList as _renderList", "Fragment as _Fragment" }, Constants.VUE_IMPORT_NAME));
                return ret.ToArray();
            }
        }
    }
}
