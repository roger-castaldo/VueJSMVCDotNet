using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles
{
    internal class VueFileTokenizer
    {
        private string _content;
        
        public VueFileTokenizer(TextReader tr)
        {
            _content = tr.ReadToEnd();
        }

        public VueFileTokenizer(string content)
        {
            _content = content;
        }

        private static readonly Regex _regTag = new Regex(@"\<(/?)([^\s/>]+)(\s+([^\s=/>]+)\s*(=\s*""([^""]+)""))*(/?)\>", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public ITokenSection[] Tokenize()
        {
            List<ITokenSection> ret = new List<ITokenSection>();
            MatchCollection matches = _regTag.Matches(_content);
            int sidx = 0;
            for(int x = 0; x<matches.Count; x++)
            {
                switch (matches[x].Value)
                {
                    case "<template>":
                        ret.Add(new TemplateSection(_ProcessTemplate(matches,ref x)));
                        break;
                    case "<script>":
                        sidx = matches[x].Index+matches[x].Length;
                        x++;
                        while (matches[x].Value!="</script>")
                            x++;
                        ret.Add(new ScriptSection(false, _content.Substring(sidx, matches[x].Index-sidx)));
                        break;
                    case "<script setup>":
                        sidx = matches[x].Index+matches[x].Length;
                        x++;
                        while (matches[x].Value!="</script>")
                            x++;
                        ret.Add(new ScriptSection(true, _content.Substring(sidx, matches[x].Index-sidx)));
                        break;
                    case "<style>":
                        sidx = matches[x].Index+matches[x].Length;
                        x++;
                        while (matches[x].Value!="</style>")
                            x++;
                        ret.Add(new StyleSection(_content.Substring(sidx, matches[x].Index-sidx)));
                        break;
                }
            }
            ret.Sort();
            return ret.ToArray();
        }

        private IToken[] _ProcessTemplate(MatchCollection matches, ref int x)
        {
            x++;
            List<IToken> ret = new List<IToken>();
            while (x<matches.Count)
            {

                if (matches[x].Value=="</template>")
                    break;
                ret.Add(_ProcessHtmlTag(matches, ref x));
            }
            return ret.ToArray();
        }

        private IToken _ProcessHtmlTag(MatchCollection matches, ref int x)
        {
            HTMLElement elem = new HTMLElement(matches[x].Groups[2].Value);
            if (matches[x].Groups[3].Value!="")
            {
                for (int y = 3; y<matches[x].Groups.Count-1; y+=4)
                {
                    if (matches[x].Groups[y+1].Value.StartsWith("v-")||matches[x].Groups[y+1].Value.StartsWith(":"))
                        elem.Add(VueDirective.ConstructDirective(matches[x].Groups[y+1].Value, matches[x].Groups[y+3].Value));
                    else
                        elem.Add(new HTMLAttribute(matches[x].Groups[y+1].Value));
                }
            }
            if (matches[x].Groups[matches[x].Groups.Count-1].Value!="/")
            {
                int sidx = matches[x].Index+matches[x].Length;
                x++;
                while (matches[x].Value!="</"+elem.Tag+">")
                {
                    elem.Add(_ProcessTextContent(_content.Substring(sidx, matches[x].Index-sidx).Trim()));
                    elem.Add(_ProcessHtmlTag(matches, ref x));
                }
                x++;
            }
            else
                x++;
            return elem;
        }

        private IToken[] _ProcessTextContent(string content)
        {
            List<IToken> tokens = new List<IToken>();
            if (content!="")
            {
                string chunk = "";
                for (int y = 0; y<content.Length; y++)
                {
                    switch (content[y])
                    {
                        case '{':
                            if (y+1<content.Length && content[y+1]=='{')
                            {
                                if (chunk.Trim()!="")
                                    tokens.Add(new TextToken(chunk.Trim()));
                                chunk="";
                                y+=2;
                                while (!(content[y]=='}'&&content[y+1]=='}'))
                                {
                                    chunk+=content[y];
                                    y++;
                                }
                                tokens.Add(new VariableChunk(chunk));
                                y+=2;
                                chunk="";
                            }
                            else
                                chunk+=content[y];
                            break;
                        default:
                            chunk+=content[y];
                            break;
                    }
                }
                if (chunk.Trim()!="")
                    tokens.Add(new TextToken(chunk));
            }
            return tokens.ToArray();
        }
    }
}
