using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Security.Cryptography;
using VueJSMVCDotNet.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace VueJSMVCDotNet.Handlers
{
    internal class VueFilesHandler : RequestHandlerBase
    {

        private static readonly Regex regImport = new(@"^\s*import([^""']+)(""([^""]+)""|'([^']+)');?\s*$", RegexOptions.Multiline|RegexOptions.Compiled,TimeSpan.FromMilliseconds(500));
        private static readonly Regex regImportExtensions = new(@"^.+\.(js|vue)$", RegexOptions.Compiled|RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));
        private static readonly Regex regImportParts = new(@"\{([^\}]+)\}", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));
        private static readonly Regex regInlineImport = new(@"\s*import\((""[^""]+\.js""|'[^']+\.js')\)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

        private readonly struct SVueFile
        {
            public string Name { get; private init; }
            private readonly string content;
            public DateTimeOffset LastModified { get; private init; }

            public SVueFile(IFileInfo f)
            {
                Name=f.Name;
                LastModified=f.LastModified;
                StreamReader sr = new(f.CreateReadStream());
                content=sr.ReadToEnd().Replace("\\","\\\\").Replace("`", "\\`").Replace("${","\\${");
                sr.Close();
            }

            public IEnumerable<string> Imports
                => regImport.Matches(content)
                .OfType<Match>()
                .Select(m =>
                {
                    var import = (string.IsNullOrEmpty(m.Groups[3].Value) ? m.Groups[4].Value : m.Groups[3].Value);
                    if (import.EndsWith("/"))
                    {
                        var subMatch = regImportParts.Match(m.Groups[1].Value);
                        return subMatch.Success ? string.Concat(import.AsSpan(0, import.Length-1), ".js") : import;
                    }
                    else if (regImportExtensions.IsMatch(import)||!import.Contains('.'))
                        return import;
                    else
                        return "";
                })
                .Where(s=>!string.IsNullOrEmpty(s));
            
            internal string FormatCache(string absolutePath,bool isFolder, Func<string, bool> isModelUrl)
            {
                var fixedContent = regImport.Replace(content, (m) => {
                    var import = (m.Groups[3].Value=="" ? m.Groups[4].Value : m.Groups[3].Value);
                    if (import.EndsWith(".vue"))
                        return $"import{m.Groups[1].Value}'{MergeUrl(absolutePath, import, isFolder)}';";
                    else if (import.EndsWith("/"))
                    {
                        var subMatch = regImportParts.Match(m.Groups[1].Value);
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
                fixedContent = regInlineImport.Replace(fixedContent, (m) => m.Value.Replace(m.Groups[1].Value, $"{m.Groups[1].Value[..^3]}mjs{m.Groups[1].Value[0]}"));
                return $"cacheVueFile('{absolutePath}{Name}',`{fixedContent}`);";
            }
        }

        private readonly IFileProvider fileProvider;
        private readonly string baseURL;
        private readonly string vueImportPath;
        private readonly string vueLoaderImportPath;
        private readonly string coreImport;
        private readonly bool compressAllJS;
        private readonly Func<string, bool> isModelUrl;

        public VueFilesHandler(IFileProvider fileProvider, string baseURL,string vueImportPath, string vueLoaderImportPath,string coreImport,bool compressAllJS,Func<string,bool> isModelUrl,
            RequestDelegate next,IMemoryCache cache,ILogger log)
            : base(next,cache,log) 
        {
            this.fileProvider=fileProvider;
            this.baseURL=baseURL;
            this.vueImportPath=vueImportPath;
            this.vueLoaderImportPath=vueLoaderImportPath;
            this.coreImport=coreImport;
            this.compressAllJS=compressAllJS;
            this.isModelUrl=isModelUrl;
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            if ((context.Request.Path.StartsWithSegments(new PathString(baseURL))
                ||string.Equals(context.Request.Path,baseURL+".js",StringComparison.InvariantCultureIgnoreCase))
                && context.Request.Method=="GET"
                && context.Request.Path.ToString().ToLower().EndsWith(".js"))
            {
                string spath = context.Request.Path.ToString().ToLower();
                CachedContent? cc = this[spath];
                if (! await ReponseCached(context,cc))
                {
                    if (cc==null)
                    {
                        IEnumerable<SVueFile> files = Array.Empty<SVueFile>();
                        string absolutePath = string.Concat(spath[..^(spath.EndsWith(".min.js") ? 7 : 3)], "/");
                        string fpath = Utility.TranslatePath(fileProvider, baseURL, spath[..^(spath.EndsWith(".min.js") ? 7 : 3)]);
                        if (fpath!=null)
                            files = fileProvider.GetDirectoryContents(fpath)
                                .Where(f => f.Name.ToLower().EndsWith(".vue"))
                                .Select(f => new SVueFile(f));
                        else
                        {
                            string name = spath[(spath.LastIndexOf('/')+1)..];
                            absolutePath=spath[..(spath.LastIndexOf("/")+1)];
                            fpath = Utility.TranslatePath(fileProvider, baseURL, spath[..^name.Length]);
                            name = (name.EndsWith(".min.js") ? name[..^7] : name[..^3]).ToLower()+".vue";
                            if (fpath!=null)
                                files = fileProvider.GetDirectoryContents(fpath)
                                    .Where(f => string.Equals(f.Name,name,StringComparison.InvariantCultureIgnoreCase))
                                    .Select(f => new SVueFile(f));
                        }
                        if (files.Any())
                        {
                            StringBuilder sb = new();
                            sb.AppendLine(@$"import {{ loadModule }} from '{vueLoaderImportPath}';
import {{defineAsyncComponent}} from '{vueImportPath}';
import {{cacheVueFile, vueSFCOptions}} from '{coreImport}';");

                            var multipleFiles = files.Count()>1;

                            var imports = files.SelectMany(file => file.Imports)
                                .Where(imp => imp!=vueImportPath)
                                .Select(imp => MergeUrl(absolutePath, imp, multipleFiles))
                                .Distinct();

                            //append vue file imports
                            imports.Where(imp =>
                                !isModelUrl(imp) && (
                                    imp.Length<=4
                                    || (imp.Length>4
                                        && !string.Equals(imp[^4..], ".vue", StringComparison.InvariantCultureIgnoreCase)
                                        )
                                    )
                                )
                                .ForEach(imp => sb.AppendLine($"import * as {VueFilesHandler.ComputeKey(imp)} from '{imp}';"));

                            //append non vue file imports
                            VueFilesHandler.MergeVueImports((multipleFiles ? absolutePath.Trim('/') : baseURL),
                                imports.Where(imp =>
                                    imp.Length>3 && string.Equals(imp[^3..], "vue", StringComparison.InvariantCultureIgnoreCase)
                                ), files)
                                .ForEach(imp => sb.AppendLine($"import '{imp}';"));

                            //append vue file module caching
                            imports.Where(imp =>
                                !isModelUrl(imp) && (
                                    imp.Length<=4
                                    || (imp.Length>4
                                        && !string.Equals(imp[^4..], ".vue", StringComparison.InvariantCultureIgnoreCase)
                                        )
                                    )
                                )
                                .ForEach(url => sb.AppendLine($"vueSFCOptions.moduleCache['{url}'] = {VueFilesHandler.ComputeKey(url)};"));

                            //append file content cache
                            files.ForEach(file => sb.AppendLine(file.FormatCache(absolutePath, multipleFiles, isModelUrl)));

                            //append module definitions
                            VueFilesHandler.SortFiles(files,baseURL)
                                .ForEach(file =>
                                {
                                    var fileName = FormatFileName(file.Name);
                                    sb.AppendLine($"const {fileName} = defineAsyncComponent(() => loadModule('{absolutePath}{file.Name}', vueSFCOptions));");
                                });

                            if (files.Count()==1)
                                sb.AppendLine($"export default {FormatFileName(files.First().Name)};");
                            else
                                sb.AppendLine($"export {{{string.Join(',',files.Select(file=>FormatFileName(file.Name)))}}};");
                            
                            if (sb.Length>0)
                            {
                                sb.Length-=2;
                                cc = new()
                                {
                                    Timestamp=files.OrderByDescending(f => f.LastModified.Ticks).Last().LastModified.DateTime,
                                    Content=(compressAllJS ? JSMinifier.Minify(sb.ToString()) : sb.ToString())
                                };
                                fileProvider.Watch($"{fpath}{Path.DirectorySeparatorChar}*.vue").RegisterChangeCallback(state =>
                                {
                                    this[(string)state]=null;
                                }, spath);
                                this[spath] = cc;
                            }
                        }
                    }
                    if (cc!=null)
                        await ProduceResponse(context,"text/javascript", cc.Timestamp, (!compressAllJS && spath.EndsWith(".min.js") ? JSMinifier.Minify(cc.Content) : cc.Content));
                    else
                        await ProduceNotFound(context,"Unable to locate requested file.");
                }
            }
            else
                await next(context);
        }

        private static IEnumerable<string> MergeVueImports(string baseURL, IEnumerable<string> vueImports, IEnumerable<SVueFile> files)
            => vueImports.Where(imp => !files.Any(f => FileMatch(f.Name, imp,baseURL)))
                    .Select(imp => MergeUrl(baseURL, imp, files.Count()>1))
                    .Select(url => (url.EndsWith(".js") ? url : string.Concat(url.AsSpan(0, url.Length-4), ".js")))
                    .Distinct();

        private static string ComputeKey(string str)
            => $"_{new Guid(MD5.HashData(UTF8Encoding.UTF8.GetBytes(str))).ToString().Replace("-", "")}";

        private static List<SVueFile> SortFiles(IEnumerable<SVueFile> files,string baseURL)
        {
            var list = files.ToList();
            List<SVueFile> result = new();
            result.AddRange(list.Where(f => !f.Imports.Any()).ToArray());
            list.RemoveAll(f => !f.Imports.Any());
            result.AddRange(list.Where(f => !f.Imports.Any(i => list.Any(fi => FileMatch(fi.Name,i, baseURL)))).ToArray());
            list.RemoveAll(f => !f.Imports.Any(i => list.Any(fi => FileMatch(fi.Name,i, baseURL))));
            bool changed = true;
            while(list.Count>0 && changed)
            {
                changed=false;
                for(int x = 0; x<list.Count; x++)
                {
                    if (!list.Any(f => f.Imports.Any(i => FileMatch(list[x].Name, i, baseURL))))
                    {
                        changed=true;
                        result.Add(list[x]);
                        list.RemoveAt(x);
                    }
                }
            }
            result.AddRange(list);
            return result;
        }

        private static bool FileMatch(string name, string import,string basePath)
            => name==import || 
            string.Equals($"./{name}",import,StringComparison.InvariantCultureIgnoreCase) || 
            string.Equals($"{name[..name.LastIndexOf(".")]}.js",import, StringComparison.InvariantCultureIgnoreCase) || 
            string.Equals($"./{name[..name.LastIndexOf(".")]}.js", import, StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals($"/{basePath}/{name}",import,StringComparison.InvariantCultureIgnoreCase);

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
