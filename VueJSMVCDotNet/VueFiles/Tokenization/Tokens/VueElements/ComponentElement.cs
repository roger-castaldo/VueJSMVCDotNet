using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens.VueDirectives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens.VueElements
{
    internal class ComponentElement : AElement
    {
        private static readonly Regex _REG_VARIABLE = new Regex("[^A-Za-z_]", RegexOptions.Compiled|RegexOptions.ECMAScript);

        private IVueDirective[] NonIsDirectives => Directives.Where(di => !(di is BindDirective && ((BindDirective)di).Command=="is")).ToArray();
        private HTMLAttribute[] NonIsAttributes => Children.Where(ci => ci is HTMLAttribute && ((HTMLAttribute)ci).Name!="is").Select(ci => (HTMLAttribute)ci).ToArray();

        public ComponentElement(string tag)
            : base(tag) { }

        public override int Cost
        {
            get
            {
                int cost = 0;
                foreach (IPatchable ip in Children.Where(c => c is IPatchable).Select(c => (IPatchable)c))
                    cost=Math.Max(cost, ip.Cost);
                return cost;
            }
        }

        public override void Compile(ref StringBuilder sb, IParsedComponent[] components, string name, ref int cacheCount,bool isSetup)
        {

            if (Tag=="component")
            {
                if (Directives.Count(di => di is BindDirective && ((BindDirective)di).Command=="is")>0)
                {
                    sb.AppendFormat("(_openBlock(), _createBlock(_resolveDynamicComponent({0})", VueFileCompiler.ProcessClassProperties(
                        components,
                        Directives
                        .Where(di => di is BindDirective && ((BindDirective)di).Command=="is")
                        .Select(di => (BindDirective)di).FirstOrDefault<BindDirective>().Value
                    ));
                }
                else
                    sb.AppendFormat("(_openBlock(), _createBlock(_resolveDynamicComponent(\"{0}\")",
                        Children
                        .Where(it => it is HTMLAttribute && ((HTMLAttribute)it).Name=="is")
                        .Select(it => (HTMLAttribute)it)
                        .FirstOrDefault().Value
                    );
            }
            else
                sb.AppendFormat("_createVNode(_component_{0}", _REG_VARIABLE.Replace(Tag, "_"));

            if (NonIsAttributes.Length>0 || NonIsDirectives.Count(di => (di is BindDirective || di is FullBindDirective))>0)
            {
                sb.Append(",");
                if ((NonIsAttributes.Length>0 || NonIsDirectives.Count(di => di is BindDirective)>9) && NonIsDirectives.Count(di => di is FullBindDirective)>0)
                {
                    sb.Append("_mergeProps(");

                    sb.Append("{");

                    foreach (HTMLAttribute att in NonIsAttributes)
                    {
                        att.Compile(ref sb, components, name, ref cacheCount);
                        sb.AppendLine(",");
                    }
                    foreach (BindDirective bd in NonIsDirectives.Where(di => di is BindDirective).Select(di => (BindDirective)di))
                    {
                        bd.Compile(ref sb, components, name, ref cacheCount);
                        sb.AppendLine(",");
                    }
                    sb.Length=sb.Length-3;
                    sb.Append("}");
                    if (Directives.Count(di => di is FullBindDirective)>0)
                        sb.AppendFormat(",{0})", VueFileCompiler.ProcessClassProperties(components, Directives.Where(di => di is FullBindDirective).Select(di => (FullBindDirective)di).FirstOrDefault().Value));
                }
                else if (NonIsDirectives.Count(di => di is FullBindDirective)>0)
                    sb.AppendFormat("_normalizeProps(_guardReactiveProps({0}))", VueFileCompiler.ProcessClassProperties(components, Directives.Where(di => di is FullBindDirective).Select(di => (FullBindDirective)di).FirstOrDefault().Value));
                else
                    sb.Append("{}");
                sb.Append(",");
                if (Content.Length>0)
                {
                    sb.Append(@"{
default: _withCtx(()=>[");
                    foreach (ICompileable c in Content
                        .Where(it => it is ICompileable)
                        .Select(it => (ICompileable)it)
                        .Where(ic=>!(ic is HTMLElement)
                                ||(ic is HTMLElement && ((HTMLElement)ic).Tag!="template")
                                ||(ic is HTMLElement && ((HTMLElement)ic).Tag=="template" && ((AElement)ic).Children.Count(c=>c is HTMLAttribute && ((HTMLAttribute)c).Name=="#default")==1)))
                    {
                        c.Compile(ref sb, components, name, ref cacheCount);
                        sb.AppendLine(",");
                    }
                    sb.Length=sb.Length-3;
                    sb.AppendLine("])");
                    foreach (ICompileable c in Content
                        .Where(it => it is ICompileable)
                        .Select(it => (ICompileable)it)
                        .Where(ic => (ic is HTMLElement && ((HTMLElement)ic).Tag=="template" && ((AElement)ic).Children.Count(c => c is HTMLAttribute && ((HTMLAttribute)c).Name.StartsWith("#") && ((HTMLAttribute)c).Name!="#default")==1)))
                    {
                        sb.AppendFormat(",{0}: _withCtx(()=>[", ((AElement)c).Children.Where(ac => ac is HTMLAttribute).Select(ac => (HTMLAttribute)ac).Where(ha => ha.Name.StartsWith("#")).FirstOrDefault().Name.Substring(1));
                        c.Compile(ref sb, components, name, ref cacheCount);
                        sb.AppendLine("])");
                    }
                    sb.AppendLine("}");
                }
                else
                    sb.AppendLine("null");
                sb.AppendFormat(",{0}", Cost);
            }
            if (Tag=="component")
                sb.Append("))");
            else
                sb.Append(")");
        }

        protected override IParsedComponent[] ParsedComponents
        {
            get
            {
                List<IParsedComponent> ret = new List<IParsedComponent>(new IParsedComponent[]
                {
                    new Import(new string[]{"openBlock as _openBlock", "createBlock as _createBlock"},Constants.VUE_IMPORT_NAME)
                });
                if (Tag=="component")
                    ret.Add(new Import(new string[] { "resolveDynamicComponent as _resolveDynamicComponent" }, Constants.VUE_IMPORT_NAME));
                else {
                    ret.AddRange(new IParsedComponent[]{
                        new Import(new string[] { "resolveComponent as _resolveComponent","createVNode as _createVNode" }, Constants.VUE_IMPORT_NAME),
                        new DeclaredConstant(string.Format("_component_{0}",_REG_VARIABLE.Replace(Tag,"_")),string.Format("_resolveComponent(\"{0}\")",Tag))
                    });
                }
                if (Directives.Count(di => di is FullBindDirective)>0)
                    ret.Add(new Import(new string[] { "normalizeProps as _normalizeProps", "guardReactiveProps as _guardReactiveProps" }, Constants.VUE_IMPORT_NAME));
                if (Directives.Count(di => !(di is FullBindDirective))>0)
                    ret.Add(new Import(new string[] { "mergeProps as _mergeProps" }, Constants.VUE_IMPORT_NAME));
                if (Content.Length>0)
                    ret.Add(new Import(new string[] { "withCtx as _withCtx" }, Constants.VUE_IMPORT_NAME));
                return ret.ToArray();
            }
        }
    }
}
