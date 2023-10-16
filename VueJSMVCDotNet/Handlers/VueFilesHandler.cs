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
using Microsoft.Extensions.Logging;

namespace VueJSMVCDotNet.Handlers
{
    internal class VueFilesHandler : RequestHandlerBase
    {

        private static readonly Regex _regImport = new(@"^\s*import([^""']+)(""([^""]+)""|'([^']+)');?\s*$", RegexOptions.Multiline|RegexOptions.Compiled,TimeSpan.FromMilliseconds(500));
        private static readonly Regex _regImportExtensions = new(@"^.+\.(js|vue)$", RegexOptions.Compiled|RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));
        private static readonly Regex _regImportParts = new(@"\{([^\}]+)\}", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));
        private static readonly Regex _regInlineImport = new(@"\s*import\((""[^""]+\.js""|'[^']+\.js')\)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

        private readonly struct SVueFile
        {
            public string Name { get; private init; }
            private readonly string _content;
            public DateTimeOffset LastModified { get; private init; }

            public SVueFile(IFileInfo f)
            {
                Name=f.Name;
                LastModified=f.LastModified;
                StreamReader sr = new(f.CreateReadStream());
                _content=sr.ReadToEnd().Replace("\\","\\\\").Replace("`", "\\`").Replace("${","\\${");
                sr.Close();
            }

            public string[] Imports
            {
                get
                {
                    List<string> ret = new();
                    foreach (Match m in _regImport.Matches(_content).OfType<Match>())
                    {
                        var import = (string.IsNullOrEmpty(m.Groups[3].Value) ? m.Groups[4].Value : m.Groups[3].Value);
                        if (import.EndsWith("/"))
                        {
                            var subMatch = _regImportParts.Match(m.Groups[1].Value);
                            if (subMatch.Success)
                                ret.Add(string.Concat(import.AsSpan(0,import.Length-1), ".js"));
                            else
                                ret.Add(import);
                        }
                        else if (_regImportExtensions.IsMatch(import)||!import.Contains('.'))
                            ret.Add(import);
                    }
                    return ret.ToArray();
                }
            }

            internal string FormatCache(string absolutePath,bool isFolder, Func<string, bool> isModelUrl)
            {
                var fixedContent = _regImport.Replace(_content, (m) => {
                    var import = (m.Groups[3].Value=="" ? m.Groups[4].Value : m.Groups[3].Value);
                    if (import.EndsWith(".vue"))
                        return $"import{m.Groups[1].Value}'{MergeUrl(absolutePath, import, isFolder)}';";
                    else if (import.EndsWith("/"))
                    {
                        var subMatch = _regImportParts.Match(m.Groups[1].Value);
                        if (subMatch.Success)
                        {
                            var sb = new StringBuilder();
                            foreach (var imp in subMatch.Groups[1].Value.Split(',').Where(i => !string.IsNullOrEmpty(i.Trim())))
                                sb.AppendLine($"import {imp.Trim()} from '{MergeUrl(absolutePath, $"{import}{imp.Trim()}.vue", isFolder)}';");
                            return sb.ToString();
                        }
                        else
                            return m.Value;
                    }
                    else if (isModelUrl(import))
                        return (import.EndsWith(".js", StringComparison.InvariantCultureIgnoreCase) ? m.Value.Replace(import,$"{import[..^2]}mjs") : m.Value);
                    else
                        return m.Value;
                });
                fixedContent = _regInlineImport.Replace(fixedContent, (m) => m.Value.Replace(m.Groups[1].Value, $"{m.Groups[1].Value[..^3]}mjs{m.Groups[1].Value[0]}"));
                return $"cacheVueFile('{absolutePath}{Name}',`{fixedContent}`);";
            }
        }

        private readonly IFileProvider _fileProvider;
        private readonly string _baseURL;
        private readonly string _vueImportPath;
        private readonly string _vueLoaderImportPath;
        private readonly string _coreImport;
        private readonly bool _compressAllJS;
        private readonly Func<string, bool> _isModelUrl;

        public VueFilesHandler(IFileProvider fileProvider, string baseURL,string vueImportPath, string vueLoaderImportPath,string coreImport,bool compressAllJS,Func<string,bool> isModelUrl,
            RequestDelegate next,IMemoryCache cache,ILogger log)
            : base(next,cache,log) 
        {
            _fileProvider=fileProvider;
            _baseURL=baseURL;
            _vueImportPath=vueImportPath;
            _vueLoaderImportPath=vueLoaderImportPath;
            _coreImport=coreImport;
            _compressAllJS=compressAllJS;
            _isModelUrl=isModelUrl;
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
                            context.Response.Headers.Add("etag", $"\"{BitConverter.ToString(MD5.HashData(System.Text.ASCIIEncoding.ASCII.GetBytes(cc.Value.Timestamp.ToUniversalTime().ToString("R")))).Replace("-", "").ToLower()}\"");
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
                        List<SVueFile> files = new();
                        IDirectoryContents contents = null;
                        string absolutePath = string.Concat(spath[..^(spath.EndsWith(".min.js") ? 7 : 3)], "/");
                        string fpath = Utility.TranslatePath(_fileProvider, _baseURL, spath[..^(spath.EndsWith(".min.js") ? 7 : 3)]);
                        if (fpath!=null)
                        {
                            contents = _fileProvider.GetDirectoryContents(fpath);
                            foreach (IFileInfo f in contents.Where(f => f.Name.ToLower().EndsWith(".vue")))
                                files.Add(new SVueFile(f));
                        }
                        else
                        {
                            string name = spath[(spath.LastIndexOf('/')+1)..];
                            absolutePath=spath[..(spath.LastIndexOf("/")+1)];
                            fpath = Utility.TranslatePath(_fileProvider, _baseURL, spath[..^name.Length]);
                            name = (name.EndsWith(".min.js") ? name[..^7] : name[..^3]).ToLower()+".vue";
                            if (fpath!=null)
                            {
                                contents = _fileProvider.GetDirectoryContents(fpath);
                                foreach (IFileInfo f in contents.Where(f => f.Name.ToLower()==name))
                                    files.Add(new SVueFile(f));
                            }
                        }
                        if (files.Count>0)
                        {
                            IEnumerable<string> imports = Array.Empty<string>();
                            foreach (SVueFile file in files)
                            {
                                imports = imports.Concat(file.Imports);
                            }

                            imports = imports.Where(imp=>imp!=_vueImportPath).Select(imp=> MergeUrl(absolutePath, imp, files.Count>1)).Distinct();

                            StringBuilder sb = new();
                            sb.AppendLine(@$"import {{ loadModule }} from '{_vueLoaderImportPath}';
import {{defineAsyncComponent}} from '{_vueImportPath}';
import {{cacheVueFile, vueSFCOptions}} from '{_coreImport}';");

                            foreach (string str in imports.Where(imp=>
                                !_isModelUrl(imp) && (
                                    imp.Length<=4
                                    || (imp.Length>4 
                                        && !string.Equals(imp[^4..], ".vue", StringComparison.InvariantCultureIgnoreCase)
                                        )
                                    )
                                )
                            )
                            {
                                sb.AppendLine($"import * as {VueFilesHandler.ComputeKey(str)} from '{str}';");
                            }

                            foreach (string str in VueFilesHandler.MergeVueImports(context.Request.Path.Value,imports.Where(imp =>
                                    imp.Length>3 && string.Equals(imp[^3..], "vue", StringComparison.InvariantCultureIgnoreCase)
                                ), files))
                                sb.AppendLine($"import '{str}';");

                            foreach (string str in imports.Where(imp =>
                                !_isModelUrl(imp) && (
                                    imp.Length<=4
                                    || (imp.Length>4
                                        && !string.Equals(imp[^4..], ".vue", StringComparison.InvariantCultureIgnoreCase)
                                        )
                                    )
                                )
                            )
                            {
                                var key = VueFilesHandler.ComputeKey(str);
                                sb.AppendLine($"vueSFCOptions.moduleCache['{str}'] = {key};");
                            }

                            foreach (var file in files)
                                sb.AppendLine(file.FormatCache(absolutePath, files.Count>1,_isModelUrl));

                            files = VueFilesHandler.SortFiles(files);

                            foreach (SVueFile file in files)
                            {
                                var key = MergeUrl(absolutePath, $"./{file.Name}", false);
                                var fileName = FormatFileName(file.Name);
                                sb.AppendLine($"const {fileName} = defineAsyncComponent(() => loadModule('{absolutePath}{file.Name}', vueSFCOptions));");
                            }
                            if (files.Count==1)
                                sb.AppendLine($"export default {FormatFileName(files[0].Name)};");
                            else
                            {
                                sb.Append("export {");
                                foreach (SVueFile file in files)
                                    sb.Append(FormatFileName(file.Name)+",");
                                sb.Length--;
                                sb.AppendLine("};");
                            }
                            if (sb.Length>0)
                            {
                                sb.Length-=2;
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
                await next(context);
        }

        private static IEnumerable<string> MergeVueImports(string baseURL, IEnumerable<string> vueImports, List<SVueFile> files)
        {
            List<string> result = new();
            var translatedURLs = vueImports.Where(imp => !files.Any(f => FileMatch(f.Name, imp)))
                                .Select(imp => MergeUrl(baseURL, imp, files.Count>1))
                                .Distinct();
            return result.Select(url => (url.EndsWith(".js") ? url : string.Concat(url.AsSpan(0, url.Length-4), ".js")));
        }

        private static string ComputeKey(string str)
        {
            return $"_{new Guid(MD5.HashData(UTF8Encoding.UTF8.GetBytes(str))).ToString().Replace("-", "")}";
        }

        private static List<SVueFile> SortFiles(List<SVueFile> files)
        {
            List<SVueFile> result = new();
            result.AddRange(files.Where(f => f.Imports.Length==0).ToArray());
            files.RemoveAll(f => f.Imports.Length==0);
            result.AddRange(files.Where(f => !f.Imports.Any(i => files.Any(fi => FileMatch(fi.Name,i)))).ToArray());
            files.RemoveAll(f => !f.Imports.Any(i => files.Any(fi => FileMatch(fi.Name,i))));
            bool changed = true;
            while(files.Count>0 && changed)
            {
                changed=false;
                for(int x = 0; x<files.Count; x++)
                {
                    if (!files.Any(f => f.Imports.Any(i => FileMatch(files[x].Name, i))))
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

        private static bool FileMatch(string name, string i)
        {
            return name==i || $"./{name}"==i || $"{name[..name.LastIndexOf(".")]}.js"==i || $"./{name[..name.LastIndexOf(".")]}.js"==i;
        }

        private static string MergeUrl(string baseUrl, string path,bool isFolder)
        {
            if (path.StartsWith("http") || !path.Contains('/') || path.StartsWith('/'))
                return path;
            if (isFolder)
            {
                if (baseUrl.EndsWith(".min.js"))
                    baseUrl = string.Concat(baseUrl[..^7], "/");
                else if (baseUrl.EndsWith(".js"))
                    baseUrl = string.Concat(baseUrl[..^3], "/");
            }
            if (!baseUrl.EndsWith('/'))
                baseUrl = baseUrl[..baseUrl.LastIndexOf('/')];
            else if (baseUrl.EndsWith('/'))
                baseUrl =baseUrl[..^1];
            while (path.StartsWith('.'))
            {
                if (path.StartsWith(".."))
                    baseUrl=baseUrl[..baseUrl.LastIndexOf('/')];
                path=path[(path.IndexOf('/')+1)..];
            }
            return $"{baseUrl}/{path}";
        }

        private static string FormatFileName(string fileName)
        {
            var result = new StringBuilder();
            var upper = true;
            foreach (char c in fileName[..^4])
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
    }
}
