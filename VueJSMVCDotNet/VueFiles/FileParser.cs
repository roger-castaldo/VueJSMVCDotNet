using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles
{
    internal class FileParser
    {
        public struct sConvertedFile
        {
            private string _definition;
            private string _additionalCode;
            private string _returns;
            private string _css;
            public string CSS { get { return _css; } }

            private bool _scopedCSS;
            public bool ScopedCSS { get { return _scopedCSS; } }

            private Dictionary<string, string> _defineMap;
            public Dictionary<string, string> DefineMap { get { return _defineMap; } }

            private string[] _defines;
            public string[] Defines { get { return _defines; } }

            private Dictionary<string, IFileInfo> _vueImports;
            public Dictionary<string, IFileInfo> VueImports { get { return _vueImports; } }

            private DateTime _modDate;
            public DateTime ModDate { get { return _modDate; } }


            private static readonly Regex _regReturns = new Regex("^returns\\s*\\(", RegexOptions.Compiled|RegexOptions.Multiline);
            internal sConvertedFile(string template, string preDefinition, string definition, string css, bool scopedCSS, Dictionary<string, string> defineMap, string[] defines, Dictionary<string, IFileInfo> vueImports, DateTime modDate)
            {
                if (template!=null)
                {
                    template = template.Substring(template.IndexOf(">")+1);
                    template = template.Substring(0, template.LastIndexOf("<"));
                    _definition = string.Format("{{template:`{0}`,{1}}}", new string[]{
                        template,
                        definition
                    });
                }
                else
                    _definition=string.Format("{{{0}}}", definition);
                _returns = "";
                if (_regReturns.IsMatch(preDefinition))
                {
                    Match m = _regReturns.Match(preDefinition);
                    for (int x = m.Index+m.Length-1; x<preDefinition.Length; x++)
                    {
                        switch (preDefinition[x])
                        {
                            case '(':
                                _returns+=preDefinition[x];
                                x++;
                                int bracketCount = 1;
                                while (bracketCount>0)
                                {
                                    _returns+=preDefinition[x];
                                    switch (preDefinition[x])
                                    {
                                        case '(':
                                            bracketCount++;
                                            break;
                                        case ')':
                                            bracketCount--;
                                            break;
                                    }
                                    if (bracketCount!=0)
                                        x++;
                                }
                                break;
                            case ';':
                                preDefinition = preDefinition.Substring(0, m.Index)+(preDefinition.Length>x+1 ? preDefinition.Substring(x+1) : "");
                                x=preDefinition.Length;
                                break;
                            default:
                                _returns+=preDefinition[x];
                                break;
                        }
                        if (_returns.EndsWith(")"))
                            _returns = _returns.Substring(0, _returns.Length-1);
                        if (_returns.StartsWith("("))
                            _returns = _returns.Substring(1);
                    }
                }
                _additionalCode = (preDefinition=="" ? null : preDefinition);
                _css=(css!=null && css!="" ? css : null);
                _scopedCSS=scopedCSS;
                _defineMap=defineMap;
                _defines=defines;
                _vueImports = vueImports;
                _modDate=modDate;
            }

            public string GetAdditionalCode(bool minify)
            {
                return (_additionalCode==null ? "" : (minify ? JSMinifier.Minify(_additionalCode) : _additionalCode));
            }
            public string GetDefinition(bool minify)
            {
                return (minify ? JSMinifier.Minify(_definition) : _definition);
            }

            public string GetReturns(bool minify)
            {
                return (minify ? JSMinifier.Minify(_returns) : _returns);
            }
        }

        private static Dictionary<string, sConvertedFile> _cache;
        private static readonly Regex _regTemplateEscape = new Regex("([^\\\\])\\$\\{", RegexOptions.Compiled|RegexOptions.ECMAScript);
        private static readonly Regex _regTemplateTag = new Regex("</?template(\\s+[^>]+)?>", RegexOptions.Compiled|RegexOptions.ECMAScript|RegexOptions.IgnoreCase);
        private static readonly Regex _regScriptTag = new Regex("</?script(\\s+[^>]+)?>", RegexOptions.Compiled|RegexOptions.ECMAScript|RegexOptions.IgnoreCase);
        private static readonly Regex _regStyleTag = new Regex("</?style(\\s+[^>]+)?>", RegexOptions.Compiled|RegexOptions.ECMAScript|RegexOptions.IgnoreCase);
        private static readonly Regex _regImport = new Regex("^\\s*import\\s+(([A-Za-z0-9_]+)\\s+from\\s+)?'([^']+)'\\s*;?\\s*?$", RegexOptions.Compiled|RegexOptions.ECMAScript|RegexOptions.IgnoreCase|RegexOptions.Multiline);
        private static readonly Regex _regExport = new Regex("^\\s*export\\s+default\\s+\\{", RegexOptions.Compiled|RegexOptions.ECMAScript|RegexOptions.IgnoreCase|RegexOptions.Multiline);
        static FileParser()
        {
            _cache = new Dictionary<string, sConvertedFile>();
        }

        public static sConvertedFile? ConvertFile(IFileInfo fi,string folderPath,string baseURL, IFileProvider fileProvider)
        {
            string spath = string.Format("{0}/{1}",new object[] {folderPath,fi.Name}).ToLower();
            sConvertedFile? ret = null;
            lock (_cache)
            {
                if (_cache.ContainsKey(spath))
                {
                    if (_cache[spath].ModDate.Ticks!=fi.LastModified.Ticks)
                        _cache.Remove(spath);
                    else
                        ret=_cache[spath];
                }
            }
            if (ret.HasValue)
                return ret.Value;
            string css = null;
            bool scopedCSS = false;
            string template = null;
            string script = null;
            List<string> imports = new List<string>();
            Dictionary<string, string> importMap = new Dictionary<string, string>();
            Dictionary<string, IFileInfo> vueImports = new Dictionary<string, IFileInfo>();
            string line;
            StreamReader sr = new StreamReader(fi.CreateReadStream());
            while ((line = sr.ReadLine())!=null)
            {
                if (_regTemplateTag.IsMatch(line))
                {
                    template = line+"\n"+_ProcessTag(ref sr, _regTemplateTag);
                    Match m = _regTemplateEscape.Match(template);
                    while (m.Success)
                    {
                        template=template.Replace(m.Value, m.Groups[1].Value+"\\${");
                        m = _regTemplateEscape.Match(template);
                    }
                }
                else if (_regScriptTag.IsMatch(line))
                    script = _ProcessTag(ref sr, _regScriptTag);
                else if (_regStyleTag.IsMatch(line))
                {
                    scopedCSS = line.ToLower().Contains(" scoped");
                    css = _ProcessTag(ref sr, _regStyleTag);
                }
            }
            sr.Close();
            if (script!=null)
            {
                script = script.Substring(0, script.LastIndexOf("<"));
                if (css!=null && css.Contains("</"))
                    css = css.Substring(0, css.LastIndexOf("</"));
                Match m = _regImport.Match(script);
                while (m.Success)
                {
                    if (m.Groups[3].Value.ToLower().EndsWith(".vue") && m.Groups[1].Value != "")
                    {
                        vueImports.Add(m.Groups[2].Value, fileProvider.GetFileInfo(Utility.TranslatePath(fileProvider,baseURL,string.Format("{0}/{2}", new object[]{
                            folderPath,
                            m.Groups[3].Value
                        }))));
                    }
                    else
                    {
                        if (m.Groups[3].Value.EndsWith(".vue"))
                        {
                            imports.Add(string.Format("{0}{1}{2}", new object[]{
                                folderPath,
                                Path.DirectorySeparatorChar,
                                (m.Groups[3].Value.StartsWith("./") ? m.Groups[3].Value.Substring(2) : m.Groups[3].Value)
                            }));
                        }
                        else
                            imports.Add(m.Groups[3].Value);
                        if (m.Groups[2].Value != "")
                            importMap.Add(m.Groups[3].Value, m.Groups[2].Value);
                    }
                    script = ((m.Index > 0 ? script.Substring(0, m.Index) : "") + script.Substring(m.Index + m.Length)).Trim();
                    m = _regImport.Match(script);
                }
                Match d = _regExport.Match(script);
                string preCode = script.Substring(0, d.Index).Trim();
                script = script.Substring(d.Index+d.Length).Trim();
                if (script.EndsWith("};"))
                    script=script.Substring(0, script.Length-2);
                else if (script.EndsWith("}"))
                    script=script.Substring(0, script.Length-1);
                ret = new sConvertedFile(template, preCode, script, css, scopedCSS, importMap, imports.ToArray(), vueImports, new DateTime(fi.LastModified.Ticks));
            }
            if (ret.HasValue)
            {
                lock (_cache)
                {
                    if (!_cache.ContainsKey(spath))
                        _cache.Add(spath, ret.Value);
                }
            }
            return ret;
        }

        private static string _ProcessTag(ref StreamReader sr, Regex tagRegex)
        {
            StringBuilder sb = new StringBuilder();
            string line = null;
            while ((line=sr.ReadLine())!=null)
            {
                if (line.Trim()!="")
                {
                    if (tagRegex.IsMatch(line))
                    {
                        MatchCollection col = tagRegex.Matches(line);
                        if (col.Count==1)
                        {
                            sb.AppendLine(line);
                            if (col[0].Value.StartsWith("</"))
                            {
                                break;
                            }
                            else
                            {
                                sb.Append(_ProcessTag(ref sr, tagRegex));
                            }
                        }
                        else
                        {
                            sb.AppendLine(line);
                            if (col.Count%2==1)
                            {
                                if (col[col.Count-1].Value.StartsWith("</"))
                                    break;
                                else
                                    sb.Append(_ProcessTag(ref sr, tagRegex));
                            }
                        }
                    }
                    else
                        sb.AppendLine(line);
                }
            }
            return sb.ToString();
        }
    }
}
