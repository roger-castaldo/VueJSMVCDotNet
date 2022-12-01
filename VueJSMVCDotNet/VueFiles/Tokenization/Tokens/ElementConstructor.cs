using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens.VueElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens
{
    internal static class ElementConstructor
    {
        public static IHTMLElement ConstructElement(string tag)
        {
            IHTMLElement ret = null;
            switch (tag)
            {
                case "slot":
                    ret = new SlotElement();
                    break;
                default:
                    ret=new HTMLElement(tag);
                    break;
            }
            return ret;
        }
    }
}
