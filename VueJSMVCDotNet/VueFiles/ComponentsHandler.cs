using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles
{
    internal class ComponentsHandler
    {
        private VirtualDirectoryInfo _baseDir;
        public VueComponentsHandler()
        {
            _baseDir = VirtualDirectoryInfo.WWWRoot.FindSubDirectory("/resources/components");
        }

        private string _ReadFile(VirtualFileInfo fi)
        {
            string ret = "";
            StreamReader sr = new StreamReader(fi.CreateReadStream());
            ret = sr.ReadToEnd().Trim();
            sr.Close();
            ret = ret.Replace("$componentname$", _ExtractComponentName(fi));
            return ret;
        }

        public async Task ProcessRequest(HttpContext context)
        {
            string path = context.Request.Path.ToString().Substring(0, context.Request.Path.ToString().Length - (context.Request.Path.ToString().EndsWith(".min.js") ? 7 : 3));
            path = path.Substring("/resources/components/".Length);
            List<string> defines = new List<string>();
            List<string> internalDefines = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool min = context.Request.Path.ToString().Contains(".min.");
            bool transmit = true;
            List<string> loadedFiles = new List<string>();
            VirtualFileInfo fi = _baseDir.FindFile(path+ ".js");
            if (fi.Exists)
            {
                if (!RequestCache.CheckCache(ref context, fi.LastWriteTime))
                {
                    fi = Utility.MapFile(path + ".json");
                    if (fi!=null)
                    {
                        sb.AppendLine(string.Format(@"vue.component('{0}',", _ExtractComponentName(fi)));
                        sb.AppendLine(_ProcessDefines(_ReadFile(fi), ref defines));
                        sb.AppendLine(");");
                    }
                    fi = Utility.MapFile(path+ ".js");
                    sb.AppendLine(_ProcessDefines(_ReadFile(fi), ref defines));
                }
                else
                    transmit=false;
            }
            else
            {
                fi = _baseDir.FindFile(path + ".vue");
                if (fi.Exists)
                {
                    if (!RequestCache.CheckCache(ref context, fi.LastWriteTime))
                    {
                        VueFileConverter.sConvertedFile? file = VueFileConverter.ConvertFile(fi);
                        if (file.HasValue)
                        {
                            _AppendVueFile(fi, ref loadedFiles, ref sb, ref defines, ref internalDefines, null, min, false);
                            string ret = file.Value.GetReturns(min);
                            if (ret!="")
                                sb.AppendLine(string.Format("return {0};", ret));
                            else
                                sb.AppendLine(string.Format("return '{0}';", _ExtractComponentName(fi)));
                        }
                    }
                    else
                        transmit=false;
                }
            }
            if (!fi.Exists)
            {
                VirtualDirectoryInfo di = _baseDir.FindSubDirectory(path);
                if (di!=null)
                {
                    if (!RequestCache.CheckCache(ref context, di.LastWriteTime))
                    {
                        sb.AppendLine("var names=[];");
                        _RecurAddDirectory(di, ref loadedFiles, ref sb, ref defines, ref internalDefines, path, min);
                        sb.AppendLine("return names;");
                    }
                    else
                        transmit=false;
                }
            }
            if (transmit)
            {
                if (sb.Length>0)
                {
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "text/javascript";
                    await context.Response.WriteAsync(_AppendDefines(sb, defines));
                }
                else
                {
                    context.Response.StatusCode = 404;
                    context.Response.ContentType = "text/text";
                    await context.Response.WriteAsync("Not Found");
                }
            }
        }

        private string _ExtractComponentName(VirtualFileInfo fi)
        {
            List<string> parts = new List<string>();
            parts.Add(fi.Name.Replace(".json", "").Replace(".js", "").Replace(".vue", "").Replace(".", "-"));
            DirectoryInfo di = fi.Directory;
            while (di.Name.ToLower() != "components")
            {
                parts.Add(di.Name);
                di = di.Parent;
            }
            parts.Reverse();
            return string.Join("-", parts.ToArray()).ToLower();
        }

        private string _ProcessDefines(string src, ref List<string> defines)
        {
            src = src.Trim();
            string ret = "";
            if (src.StartsWith("define"))
            {
                List<string> tempCalls = new List<string>();
                foreach (string str in src.Substring(src.IndexOf("[") + 1, src.IndexOf("]") - src.IndexOf("[") - 1).Split(','))
                {
                    if (str.Trim() != "")
                    {
                        tempCalls.Add(str.Trim());
                        if (!defines.Contains(str.Trim()))
                            defines.Add(str.Trim());
                    }
                }
                int idx = 0;
                src = src.Substring(src.IndexOf("function") + "function".Length).Trim();
                StringBuilder sbAssigns = new StringBuilder();
                foreach (string str in src.Substring(src.IndexOf("(") + 1, src.IndexOf(")") - src.IndexOf("(") - 1).Split(','))
                {
                    if (str.Trim() != "")
                        sbAssigns.AppendLine(string.Format("var {0} = arg{1};", new object[] { str.Trim(), defines.IndexOf(tempCalls[idx]) }));
                    idx++;
                }
                sbAssigns.AppendLine("");
                src = src.Substring(src.IndexOf("{") + 1);
                ret = sbAssigns.ToString()+src.Substring(0, src.LastIndexOf("}"));
            }
            else
                ret = src;
            return ret;
        }

        private string _AppendDefines(StringBuilder sb, List<string> defines)
        {
            StringBuilder ret = new StringBuilder();
            ret.Append("define(['vue'");
            if (defines.Count > 0)
            {
                for (int x = 0; x < defines.Count; x++)
                    ret.AppendFormat(",{0}", new object[] { defines[x] });
            }
            ret.Append("],function(vue");
            if (defines.Count > 0)
            {
                for (int x = 0; x < defines.Count; x++)
                    ret.AppendFormat(",arg{0}", new object[] { x });
            }
            ret.Append("){");
            ret.Append(sb);
            ret.Append("});");
            return ret.ToString();
        }

        private void _RecurAddDirectory(VirtualDirectoryInfo di, ref List<string> loadedFiles, ref StringBuilder sb, ref List<string> defines, ref List<string> internalDefines, string basePath, bool min)
        {
            foreach (VirtualFileInfo fi in di.GetFiles("*.json"))
            {
                sb.AppendLine(string.Format(@"names.push(vue.component('{0}',", _ExtractComponentName(fi)));
                sb.AppendLine(_ReadFile(fi));
                sb.AppendLine("));");
            }
            foreach (VirtualFileInfo fi in di.GetFiles("*.js"))
            {
                sb.AppendLine(_AppendJSFile(fi, ref loadedFiles, ref defines, ref internalDefines));
            }
            foreach (VirtualFileInfo fi in di.GetFiles("*.vue"))
            {
                _AppendVueFile(fi, ref loadedFiles, ref sb, ref defines, ref internalDefines, basePath, min, true);
            }
            foreach (VirtualDirectoryInfo d in di.GetSubDirectories())
                _RecurAddDirectory(d, ref loadedFiles, ref sb, ref defines, ref internalDefines, basePath, min);
        }

        private string _AppendJSFile(VirtualFileInfo fi, ref List<string> loadedFiles, ref List<string> defines, ref List<string> internalDefines)
        {
            if (!loadedFiles.Contains(fi.FullName))
            {
                loadedFiles.Add(fi.FullName);
                string code = _ProcessDefines(_ReadFile(fi), ref defines);
                internalDefines.Add(fi.FullName);
                return string.Format("const iarg{0} = (function(){{ {1} }})();", new object[] { internalDefines.Count-1, code });
            }
            return "";
        }

        private void _AppendVueFile(VirtualFileInfo fi, ref List<string> loadedFiles, ref StringBuilder sb, ref List<string> defines, ref List<string> internalDefines, string basePath, bool min, bool appendName)
        {
            if (loadedFiles.Contains(fi.FullName))
                return;
            loadedFiles.Add(fi.FullName);
            VueFileConverter.sConvertedFile? file = VueFileConverter.ConvertFile(fi);
            if (file!=null)
            {
                if (file.Value.CSS!=null)
                    sb.AppendLine(string.Format("vue.style('{0}',{2},`{1}`);", new object[]{
                        _ExtractComponentName(fi),
                        file.Value.CSS,
                        file.Value.ScopedCSS.ToString().ToLower()
                    }));
                foreach (string imp in file.Value.Defines)
                {
                    if (imp.EndsWith(".js"))
                    {
                        string path = _CombinePath(fi, imp);
                        if (!loadedFiles.Contains(path))
                        {
                            VirtualFileInfo jfi = new VirtualFileInfo(path);
                            if (jfi.Exists)
                                sb.AppendLine(_AppendJSFile(jfi, ref loadedFiles, ref defines, ref internalDefines));
                        }
                    }
                    else if (!imp.EndsWith(".vue") && !defines.Contains(string.Format("'{0}'", imp)))
                    {
                        defines.Add(string.Format("'{0}'", imp));
                    }
                    else if (imp.EndsWith(".vue"))
                    {
                        if (basePath==null || !imp.StartsWith(basePath))
                        {
                            VirtualFileInfo f = new VirtualFileInfo(imp);
                            if (f.Exists)
                                _AppendVueFile(f, ref loadedFiles, ref sb, ref defines, ref internalDefines, basePath, min, appendName);
                        }
                    }
                }
                foreach (string imp in file.Value.DefineMap.Keys)
                {
                    if (defines.Contains(string.Format("'{0}'", _CombineURI(basePath, imp))))
                        sb.AppendLine(string.Format("var {0} = arg{1};", new object[]{
                            file.Value.DefineMap[imp],
                            defines.IndexOf(string.Format("'{0}'",_CombineURI(basePath, imp)))
                        }));
                    else
                    {
                        string path = _CombinePath(fi, imp);
                        if (internalDefines.Contains(path))
                            sb.AppendLine(string.Format("var {0} = iarg{1};", new object[]
                            {
                                file.Value.DefineMap[imp],
                                internalDefines.IndexOf(path)
                            }));
                    }
                }
                foreach (string key in file.Value.VueImports.Keys)
                {
                    if (basePath!=null && file.Value.VueImports[key].FullName.StartsWith(basePath))
                        sb.AppendFormat(" var {0} = '{1}'", new object[]{
                            key,
                            _ExtractComponentName(file.Value.VueImports[key])
                        });
                    else
                        _AppendVueFile(file.Value.VueImports[key], ref loadedFiles, ref sb, ref defines, ref internalDefines, basePath, min, appendName);
                }
                sb.AppendLine(file.Value.GetAdditionalCode(min));
                sb.AppendLine(string.Format((appendName ? @"names.push(vue.component('{0}',{1}));" : @"vue.component('{0}',{1});"), new object[]{
                    _ExtractComponentName(fi),
                    file.Value.GetDefinition(min)
                }));
            }
        }

        private string _CombinePath(VirtualFileInfo fi, string path)
        {
            FileInfo f = new FileInfo(Path.Combine(fi.FullName.Substring(0, fi.FullName.LastIndexOf(Path.DirectorySeparatorChar))+Path.DirectorySeparatorChar.ToString(), path.Replace('/', Path.DirectorySeparatorChar)));
            return (f.Exists ? f.FullName : "");
        }

        private string _CombineURI(string path, string url)
        {
            if (url.StartsWith("."))
            {
                List<string> tmp = new List<string>(path.Split('/'));
                while (url.StartsWith("../"))
                {
                    url = url.Substring(3);
                    tmp.RemoveAt(tmp.Count - 1);
                }
                if (url.StartsWith("./"))
                    url = url.Substring(2);
                string ret = "";
                foreach (string str in tmp)
                    ret += "/" + str;
                if (url != "")
                    ret += (url.StartsWith("/") ? "" : "/") + url;
                return "components"+ret;
            }
            return url;
        }
    }
}
