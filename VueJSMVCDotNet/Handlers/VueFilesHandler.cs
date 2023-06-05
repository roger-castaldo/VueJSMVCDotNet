using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;
using VueJSMVCDotNet.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace VueJSMVCDotNet.Handlers
{
    internal class VueFilesHandler : RequestHandlerBase
    {

        private static readonly Regex _regImport = new Regex(@"^\s*import([^""']+)(""([^""]+)""|'([^']+)');?\s*$", RegexOptions.Multiline|RegexOptions.Compiled);
        private static readonly Regex _regImportExtensions = new Regex(@"^.+\.(js|vue)$", RegexOptions.Compiled|RegexOptions.IgnoreCase);
        private static readonly Regex _regImportParts = new Regex(@"\{([^\}]+)\}", RegexOptions.Compiled);

        private struct sVueFile
        {
            private readonly string _name;
            public string Name => _name;
            private readonly string _content;
            private readonly DateTimeOffset _lastModified;
            public DateTimeOffset LastModified => _lastModified;

            public sVueFile(IFileInfo f)
            {
                _name=f.Name;
                _lastModified=f.LastModified;
                StreamReader sr = new StreamReader(f.CreateReadStream());
                _content=sr.ReadToEnd().Replace("\\","\\\\").Replace("`", "\\`").Replace("${","\\${");
                sr.Close();
            }

            public string[] Imports
            {
                get
                {
                    List<string> ret = new List<string>();
                    foreach (Match m in _regImport.Matches(_content))
                    {
                        var import = (m.Groups[3].Value=="" ? m.Groups[4].Value : m.Groups[3].Value);
                        if (import.EndsWith("/"))
                        {
                            var subMatch = _regImportParts.Match(m.Groups[1].Value);
                            if (subMatch.Success)
                                ret.Add(import.Substring(0,import.Length-1)+".js");
                            else
                                ret.Add(import);
                        }
                        else if (_regImportExtensions.IsMatch(import)||!import.Contains("."))
                            ret.Add(import);
                    }
                    return ret.ToArray();
                }
            }

            internal string FormatCache(string absolutePath,bool isFolder)
            {
                var fixedContent = _regImport.Replace(_content, (m) => {
                    var import = (m.Groups[3].Value=="" ? m.Groups[4].Value : m.Groups[3].Value);
                    if (import.EndsWith(".vue"))
                        return $"import{m.Groups[1].Value}'{_MergeUrl(absolutePath, import, isFolder)}';";
                    else if (import.EndsWith("/"))
                    {
                        var subMatch = _regImportParts.Match(m.Groups[1].Value);
                        if (subMatch.Success)
                        {
                            var sb = new StringBuilder();
                            foreach (var imp in subMatch.Groups[1].Value.Split(',').Where(i=>!string.IsNullOrEmpty(i.Trim())))
                                sb.AppendLine($"import {imp.Trim()} from '{_MergeUrl(absolutePath, $"{import}{imp.Trim()}.vue", isFolder)}';");
                            return sb.ToString();
                        }
                        else
                            return m.Value;
                    }
                    else
                        return m.Value;
                });
                return $"cacheVueFile('{absolutePath}{Name}',`{fixedContent}`);";
            }
        }

        private readonly IFileProvider _fileProvider;
        private readonly string _baseURL;
        private readonly ILog _log;
        private readonly string _vueImportPath;
        private readonly string _vueLoaderImportPath;
        private readonly string _coreImport;
        private readonly bool _compressAllJS;

        public VueFilesHandler(IFileProvider fileProvider, string baseURL, ILog log,string vueImportPath, string vueLoaderImportPath,string coreImport,bool compressAllJS,RequestDelegate next,IMemoryCache cache)
            : base(next,cache,log) 
        {
            _fileProvider=fileProvider;
            _baseURL=baseURL;
            _log=log;
            _vueImportPath=vueImportPath;
            _vueLoaderImportPath=vueLoaderImportPath;
            _coreImport=coreImport;
            _compressAllJS=compressAllJS;
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            if ((context.Request.Path.StartsWithSegments(new PathString(_baseURL))
                ||string.Equals(context.Request.Path,_baseURL+".js",StringComparison.InvariantCultureIgnoreCase))
                && context.Request.Method=="GET"
                && context.Request.Path.ToString().ToLower().EndsWith(".js"))
            {
                string spath = context.Request.Path.ToString().ToLower();
                CachedContent? cc = this[spath];
                bool respond = true;
                if (cc!=null)
                {
                    if (context.Request.Headers.ContainsKey("If-Modified-Since"))
                    {
                        if (cc.Value.Timestamp.ToUniversalTime().ToString("R").ToLower()==context.Request.Headers["If-Modified-Since"].ToString().ToLower())
                        {
                            context.Response.ContentType="text/javascript";
                            context.Response.Headers.Add("accept-ranges", "bytes");
                            context.Response.Headers.Add("date", cc.Value.Timestamp.ToUniversalTime().ToString("R"));
                            context.Response.Headers.Add("etag", $"\"{BitConverter.ToString(System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(cc.Value.Timestamp.ToUniversalTime().ToString("R")))).Replace("-", "").ToLower()}\"");
                            context.Response.StatusCode = 304;
                            await context.Response.WriteAsync("");
                            respond=false;
                        }
                    }
                }
                if (respond)
                {
                    if (!cc.HasValue)
                    {
                        List<sVueFile> files = new List<sVueFile>();
                        IDirectoryContents contents = null;
                        string absolutePath = spath.Substring(0, spath.Length-(spath.EndsWith(".min.js") ? 7 : 3))+"/";
                        string fpath = Utility.TranslatePath(_fileProvider, _baseURL, spath.Substring(0, spath.Length-(spath.EndsWith(".min.js") ? 7 : 3)));
                        if (fpath!=null)
                        {
                            contents = _fileProvider.GetDirectoryContents(fpath);
                            foreach (IFileInfo f in contents.Where(f => f.Name.ToLower().EndsWith(".vue")))
                                files.Add(new sVueFile(f));
                        }
                        else
                        {
                            string name = spath.Substring(spath.LastIndexOf('/')+1);
                            absolutePath=spath.Substring(0,spath.LastIndexOf("/")+1);
                            fpath = Utility.TranslatePath(_fileProvider, _baseURL, spath.Substring(0, spath.Length-name.Length));
                            name = (name.EndsWith(".min.js") ? name.Substring(0, name.Length-7) : name.Substring(0, name.Length-3)).ToLower()+".vue";
                            if (fpath!=null)
                            {
                                contents = _fileProvider.GetDirectoryContents(fpath);
                                foreach (IFileInfo f in contents.Where(f => f.Name.ToLower()==name))
                                    files.Add(new sVueFile(f));
                            }
                        }
                        if (files.Count>0)
                        {
                            IEnumerable<string> imports = Array.Empty<string>();
                            foreach (sVueFile file in files)
                            {
                                imports = imports.Concat(file.Imports);
                            }

                            imports = imports.Where(imp=>imp!=_vueImportPath).Select(imp=> _MergeUrl(absolutePath, imp, files.Count>1)).Distinct();

                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine(@$"import {{ loadModule }} from '{_vueLoaderImportPath}';
import {{defineAsyncComponent}} from '{_vueImportPath}';
import {{cacheVueFile, vueSFCOptions}} from '{_coreImport}';");

                            foreach (string str in imports.Where(imp=>
                                imp.Length<=4
                                || (imp.Length>4 
                                    && !string.Equals(imp.Substring(imp.Length-4), ".vue", StringComparison.InvariantCultureIgnoreCase)
                                    )
                                )
                            )
                            {
                                sb.AppendLine($"import * as {_ComputeKey(str)} from '{str}';");
                            }

                            foreach (string str in _MergeVueImports(context.Request.Path.Value,imports.Where(imp =>
                                    imp.Length>3 && string.Equals(imp.Substring(imp.Length-3), "vue", StringComparison.InvariantCultureIgnoreCase)
                                ), files))
                                sb.AppendLine($"import '{str}';");

                            foreach (string str in imports.Where(imp => 
                                imp.Length<=4
                                || (imp.Length>4
                                    && !string.Equals(imp.Substring(imp.Length-4), ".vue", StringComparison.InvariantCultureIgnoreCase)
                                    )
                                )
                            )
                            {
                                var key = _ComputeKey(str);
                                sb.AppendLine($"vueSFCOptions.moduleCache['{str}'] = {key};");
                            }

                            foreach (var file in files)
                                sb.AppendLine(file.FormatCache(absolutePath, files.Count>1));

                            files = _SortFiles(files);

                            foreach (sVueFile file in files)
                            {
                                var key = _MergeUrl(absolutePath, $"./{file.Name}", false);
                                var fileName = _FormatFileName(file.Name);
                                sb.AppendLine($"const {fileName} = defineAsyncComponent(() => loadModule('{absolutePath}{file.Name}', vueSFCOptions));");
                            }
                            if (files.Count==1)
                                sb.AppendLine($"export default {_FormatFileName(files[0].Name)};");
                            else
                            {
                                sb.Append("export {");
                                foreach (sVueFile file in files)
                                    sb.Append(_FormatFileName(file.Name)+",");
                                sb.Length=sb.Length-1;
                                sb.AppendLine("};");
                            }
                            if (sb.Length>0)
                            {
                                sb.Length=sb.Length-2;
                                cc = new CachedContent(files.OrderByDescending(f=>f.LastModified.Ticks).Last().LastModified, (_compressAllJS ? JSMinifier.Minify(sb.ToString()) : sb.ToString()));
                                _fileProvider.Watch(fpath+Path.DirectorySeparatorChar+"*.vue").RegisterChangeCallback(state =>
                                {
                                    this[(string)state]=null;
                                }, spath);
                                this[spath] = cc;
                            }
                        }
                    }
                    if (cc.HasValue)
                    {
                        context.Response.Headers.Add("Cache-Control", "public, must-revalidate, max-age=3600");
                        context.Response.Headers.Add("Last-Modified", cc.Value.Timestamp.ToUniversalTime().ToString("R"));
                        context.Response.ContentType = "text/javascript";
                        await context.Response.WriteAsync((!_compressAllJS && spath.EndsWith(".min.js") ? JSMinifier.Minify(cc.Value.Content) : cc.Value.Content));
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("Unable to locate requested file.");
                    }
                }
            }
            else
                await _next(context);
        }

        private IEnumerable<string> _MergeVueImports(string baseURL, IEnumerable<string> vueImports, List<sVueFile> files)
        {
            List<string> result = new List<string>();
            var translatedURLs = vueImports.Where(imp => !files.Any(f => _FileMatch(f.Name, imp)))
                                .Select(imp => _MergeUrl(baseURL, imp, files.Count>1))
                                .Distinct();
            return result.Select(url => (url.EndsWith(".js") ? url : url.Substring(0, url.Length-4)+".js"));
        }

        private string _ComputeKey(string str)
        {
            return $"_{new Guid(MD5.Create().ComputeHash(UTF8Encoding.UTF8.GetBytes(str))).ToString().Replace("-", "")}";
        }

        private List<sVueFile> _SortFiles(List<sVueFile> files)
        {
            List<sVueFile> result = new List<sVueFile>();
            result.AddRange(files.Where(f => f.Imports.Length==0).ToArray());
            files.RemoveAll(f => f.Imports.Length==0);
            result.AddRange(files.Where(f => !f.Imports.Any(i => files.Any(fi => _FileMatch(fi.Name,i)))).ToArray());
            files.RemoveAll(f => !f.Imports.Any(i => files.Any(fi => _FileMatch(fi.Name,i))));
            bool changed = true;
            while(files.Count>0 && changed)
            {
                changed=false;
                for(int x = 0; x<files.Count; x++)
                {
                    if (!files.Any(f => f.Imports.Any(i => _FileMatch(files[x].Name, i))))
                    {
                        changed=true;
                        result.Add(files[x]);
                        files.RemoveAt(x);
                    }
                }
            }
            result.AddRange(files);
            return result;
        }

        private static bool _FileMatch(string name, string i)
        {
            return name==i || $"./{name}"==i || $"{name.Substring(0, name.LastIndexOf("."))}.js"==i || $"./{name.Substring(0, name.LastIndexOf("."))}.js"==i;
        }

        private static string _MergeUrl(string baseUrl, string path,bool isFolder)
        {
            if (path.StartsWith("http") || !path.Contains('/') || path.StartsWith('/'))
                return path;
            if (isFolder)
            {
                if (baseUrl.EndsWith(".min.js"))
                    baseUrl = baseUrl.Substring(0, baseUrl.Length-7)+"/";
                else if (baseUrl.EndsWith(".js"))
                    baseUrl = baseUrl.Substring(0, baseUrl.Length-3)+"/";
            }
            if (!baseUrl.EndsWith('/'))
                baseUrl = baseUrl.Substring(0, baseUrl.LastIndexOf('/'));
            else if (baseUrl.EndsWith('/'))
                baseUrl =baseUrl.Substring(0, baseUrl.Length-1);
            while (path.StartsWith('.'))
            {
                if (path.StartsWith(".."))
                    baseUrl=baseUrl.Substring(0, baseUrl.LastIndexOf('/'));
                path=path.Substring(path.IndexOf('/')+1);
            }
            return $"{baseUrl}/{path}";
        }

        private static string _FormatFileName(string fileName)
        {
            var result = new StringBuilder();
            var upper = true;
            foreach (char c in fileName.Substring(0, fileName.Length-4))
            {
                switch (c)
                {
                    case '.':
                    case '-':
                    case '_':
                        upper=true;
                        break;
                    default:
                        if (upper)
                        {
                            result.Append(c.ToString().ToUpper());
                            upper=false;
                        }
                        else
                            result.Append(c);
                        break;
                }
            }
            return result.ToString();
        }

        protected override void _dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
