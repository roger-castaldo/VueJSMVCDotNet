using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens.VueElements
{
    internal class SlotElement : AElement
    {

        public SlotElement()
            : base("slot") { }

        public override int Cost => 0;

        public override void Compile(ref StringBuilder sb, IParsedComponent[] components, string name, ref int cacheCount)
        {
            var attName = Children.Where(it => it is HTMLAttribute && ((HTMLAttribute)it).Name=="name").FirstOrDefault();
            sb.AppendFormat("_renderSlot(_ctx.$slots,\"{0}\"",new object[]
            {
                (attName==null ? "default" : ((HTMLAttribute)attName).Value)
            });
            if (Content.Length>0)
            {
                sb.Append(",{},()=>[");
                foreach (IToken it in Content.Where(t=>t is ICompileable))
                    ((ICompileable)it).Compile(ref sb, components, name, ref cacheCount);
                sb.AppendLine("])");
            }else
                sb.AppendLine(")");
        }

        protected override IParsedComponent[] ParsedComponents => new IParsedComponent[]
                {
                    new Import(new string[]{"renderSlot as _renderSlot"},"vue")
                };
    }
}
