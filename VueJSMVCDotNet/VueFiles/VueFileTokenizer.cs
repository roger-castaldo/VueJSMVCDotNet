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
        private const char EOF = (char)0;

        private string _template;
        private int _index;
        private char _lastChar;
        private char _curChar = EOF;
        private char _nextChar;
        private string _curChunk;

        public VueFileTokenizer(TextReader tr)
        {
            _template = tr.ReadToEnd();
        }

        public VueFileTokenizer(string content)
        {
            _template = content;
        }

        private void Next()
        {
            if (_index==_template.Length)
            {
                _curChar = EOF;
                return;
            }
            _lastChar = _curChar;
            _curChar = _template[_index];
            _index++;
            if (_index == _template.Length)
                _nextChar = EOF;
            else
                _nextChar = _template[_index];
        }

        private void Consume()
        {
            _curChunk += _curChar;
            Next();
        }

        private void ConsumeBracket(char startBracket)
        {
            Consume();
            char exitChar = '}';
            switch (startBracket)
            {
                case '[':
                    exitChar=']';
                    break;
                case '(':
                    exitChar=')';
                    break;
            }
            while ((_curChar!=exitChar)&&(_curChar!=EOF))
            {
                if ((_curChar == '(') || (_curChar == '{') || (_curChar == '['))
                    ConsumeBracket(_curChar);
                else
                    Consume();
            }
            if (_curChar!=EOF || _curChar==exitChar)
                Consume();
        }

        public ITokenSection[] Tokenize()
        {
            List<ITokenSection> ret = new List<ITokenSection>();
            _curChunk = "";
            _index = 0;
            Next();
            while (_curChar != EOF)
            {
                if (_curChar!='>')
                    Consume();
                else
                {
                    Consume();
                    switch (_curChunk.Trim().ToLower())
                    {
                        case "<template>":
                            _curChunk="";
                            ret.Add(new TemplateSection(_ProcessHtmlTagContent("template")));
                            break;
                        case "<script>":
                            _curChunk="";
                            ret.Add(new ScriptSection(false,_ProcessTextContent("script")));
                            break;
                        case "<script setup>":
                            _curChunk="";
                            ret.Add(new ScriptSection(true, _ProcessTextContent("script")));
                            break;
                        case "<style>":
                            _curChunk="";
                            ret.Add(new StyleSection(_ProcessTextContent("style")));
                            break;
                    }
                }

            }
            ret.Sort();
            return ret.ToArray();
        }

        private IToken[] _ProcessTextContent(string tag)
        {
            List<IToken> ret = new List<IToken>();
            bool exit = false;
            while (_curChar!=EOF && !exit)
            {
                if (_curChar=='<')
                {
                    int idx = _index;
                    while (_curChar!=EOF && _curChar!=' ' && _curChar!='>' && _curChar!='\n')
                        Consume();
                    if (_curChunk.Trim().EndsWith(string.Format("</{0}>", tag)))
                    {
                        _curChunk = _curChunk.Substring(0, idx).Trim();
                        exit=true;
                    }
                }
                else
                    Consume();
            }
            if (_curChunk.Trim().EndsWith(String.Format("</{0}>", tag)))
                _curChunk = _curChunk.Substring(0, _curChunk.IndexOf(String.Format("</{0}>", tag)));
            if (_curChunk.Trim()!="")
                ret.Add(new TextToken(_curChunk.Trim()));
            return ret.ToArray();

        }

        private IToken[] _ProcessHtmlTagContent(string tag)
        {
            List<IToken> ret = new List<IToken>();
            bool inTag = false;
            bool exit = false;
            while (_curChar != EOF && !exit)
            {
                switch (_curChar)
                {
                    case '<':
                        if (!inTag && _curChunk.Trim().Length>0)
                        {
                            ret.Add(new TextToken(_curChunk.Trim()));
                            _curChunk="";
                        }
                        else if (_curChunk.Length>0)
                            _curChunk="";
                        inTag = true;
                        break;
                    case ' ':
                        if (inTag)
                        {
                            ret.Add(_ProcessHtmlTag());
                            inTag=false;
                        }
                        break;
                    case '>':
                        if (_curChunk.Trim().ToLower()==String.Format("</{0}", tag).ToLower())
                        {
                            Consume();
                            _curChunk="";
                            return ret.ToArray();
                        }
                        break;
                    case '}':
                        if (_nextChar=='}' && _curChunk.Contains("{{"))
                        {
                            Consume();
                            Consume();
                            string variable = _curChunk.Substring(_curChunk.IndexOf("{{"));
                            _curChunk=_curChunk.Substring(0, _curChunk.Length-variable.Length);
                            if (_curChunk.Trim()!="")
                                ret.Add(new TextToken(_curChunk.Trim()));
                            ret.Add(new VariableChunk(variable));
                            _curChunk="";
                        }
                        break;
                }
                Consume();
            }
            return ret.ToArray();
        }

        private readonly static Regex _regVueDirective = new Regex("^((v-|#|:)[^=]+)\\s*=\\s*\"(.+)\"$", RegexOptions.Compiled|RegexOptions.ECMAScript);

        private HTMLElement _ProcessHtmlTag()
        {
            HTMLElement ret = new HTMLElement(_curChunk.Substring(1));
            _curChunk="";
            bool exit = false;
            while (_curChar != EOF && !exit)
            {
                switch (_curChar)
                {
                    case '/':
                        if (_nextChar=='>')
                        {
                            _curChunk=_curChunk.Trim();
                            if (_curChunk.Trim()!="/")
                            {
                                _curChunk=_curChunk.TrimEnd('/');
                                if (_curChunk.Trim()!="")
                                {
                                    if (_regVueDirective.IsMatch(_curChunk))
                                    {
                                        Match m = _regVueDirective.Match(_curChunk);
                                        ret.Add(VueDirective.ConstructDirective(m.Groups[1].Value, m.Groups[3].Value));
                                    }
                                    else
                                        ret.Add(new HTMLAttribute(_curChunk));
                                }
                            }
                            Next();
                            Next();
                            _curChunk="";
                            exit=true;
                        }
                        break;
                    case '"':
                    case '\'':
                        char endQuote = _curChar;
                        Consume();
                        while (_curChar!=EOF && _curChar!=endQuote)
                            Consume();
                        break;
                    case ' ':
                        if (_curChunk.Contains("="))
                        {
                            _curChunk = _curChunk.Trim();
                            if (_regVueDirective.IsMatch(_curChunk))
                            {
                                Match m = _regVueDirective.Match(_curChunk);
                                ret.Add(VueDirective.ConstructDirective(m.Groups[1].Value, m.Groups[3].Value));
                            }
                            else
                                ret.Add(new HTMLAttribute(_curChunk));
                            _curChunk="";
                        }
                        break;
                    case '>':
                        _curChunk=_curChunk.Trim();
                        if (_curChunk!="/")
                        {
                            if (_regVueDirective.IsMatch(_curChunk))
                            {
                                Match m = _regVueDirective.Match(_curChunk);
                                ret.Add(VueDirective.ConstructDirective(m.Groups[1].Value, m.Groups[3].Value));
                            }
                            else
                                ret.Add(new HTMLAttribute(_curChunk));
                        }
                            Next();
                        _curChunk="";
                        ret.Add(_ProcessHtmlTagContent(ret.Tag));
                        exit=true;
                        break;
                }
                if (!exit)
                    Consume();
            }
            return ret;
        }
    }
}
