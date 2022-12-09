﻿using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
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
                case "a":
                case "abbr":
                case "acronym":
                case "address":
                case "applet":
                case "area":
                case "article":
                case "aside":
                case "audio":
                case "b":
                case "base":
                case "basefont":
                case "bdi":
                case "bdo":
                case "big":
                case "blockquote":
                case "body":
                case "br":
                case "button":
                case "canvas":
                case "caption":
                case "center":
                case "cite":
                case "code":
                case "col":
                case "colgroup":
                case "data":
                case "datalist":
                case "dd":
                case "del":
                case "details":
                case "dfn":
                case "dialog":
                case "dir":
                case "div":
                case "dl":
                case "dt":
                case "em":
                case "embed":
                case "fieldset":
                case "figcaption":
                case "figure":
                case "font":
                case "footer":
                case "form":
                case "frame":
                case "frameset":
                case "h1":
                case "head":
                case "header":
                case "hr":
                case "html":
                case "i":
                case "iframe":
                case "img":
                case "input":
                case "ins":
                case "kbd":
                case "label":
                case "legend":
                case "li":
                case "link":
                case "main":
                case "map":
                case "mark":
                case "meta":
                case "meter":
                case "nav":
                case "noframes":
                case "noscript":
                case "object":
                case "ol":
                case "optgroup":
                case "option":
                case "output":
                case "p":
                case "param":
                case "picture":
                case "pre":
                case "progress":
                case "q":
                case "rp":
                case "rt":
                case "ruby":
                case "s":
                case "samp":
                case "script":
                case "section":
                case "select":
                case "small":
                case "source":
                case "span":
                case "strike":
                case "strong":
                case "style":
                case "sub":
                case "summary":
                case "sup":
                case "svg":
                case "table":
                case "tbody":
                case "td":
                case "template":
                case "textarea":
                case "tfoot":
                case "th":
                case "thead":
                case "time":
                case "title":
                case "tr":
                case "track":
                case "tt":
                case "u":
                case "ul":
                case "var":
                case "video":
                case "wbr":
                    ret=new HTMLElement(tag);
                    break;
                case "slot":
                    ret = new SlotElement();
                    break;
                default:
                    ret=new ComponentElement(tag);
                    break;
            }
            return ret;
        }
    }
}
