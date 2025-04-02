using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using System.IO;
using VueJSMVCDotNet.Caching;
using VueJSMVCDotNet.Extensions;

namespace VueJSMVCDotNet.Endpoints
{
    internal class VueFilesEndpoint(IFileProvider fileProvider, string baseURL, string vueImportPath, string vueLoaderImportPath,
        string coreImport, bool compressAllJS, Func<string, bool> isModelUrl, ILogger? logger, IMemoryCache? cache)
        : ACachingEndpoint(logger, cache)
    {
        private const string PathParameter = "path";

        private static readonly Regex regImport = new(@"^\s*import([^""']+)(""([^""]+)""|'([^']+)');?\s*$", RegexOptions.Multiline|RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));
        private static readonly Regex regImportExtensions = new(@"^.+\.(js|vue)$", RegexOptions.Compiled|RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));
        private static readonly Regex regImportParts = new(@"\{([^\}]+)\}", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));
        private static readonly Regex regInlineImport = new(@"\s*import\((""[^""]+\.js""|'[^']+\.js'|\\`[^`]+\.js\\`)\)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));
        private static readonly Regex regLoadModule = new(@"\s*loadModule\((""[^""]+\.vue""|'[^']+\.vue'|\\`[^`]+\.vue\\`)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

        private readonly struct SVueFile
        {
            public string Name { get; private init; }
            public string? PhysicalPath { get; private init; }
            private readonly string content;
            public DateTimeOffset LastModified { get; private init; }

            public SVueFile(IFileInfo f)
            {
                Name=f.Name;
                PhysicalPath=f.PhysicalPath;
                LastModified=f.LastModified;
                StreamReader sr = new(f.CreateReadStream());
                content=sr.ReadToEnd().Replace("\\", "\\\\").Replace("`", "\\`").Replace("${", "\\${");
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
                .Where(s => !string.IsNullOrEmpty(s));

            internal string FormatCache(string absolutePath, bool isFolder, Func<string, bool> isModelUrl)
            {
                var fixedContent = regImport.Replace(content, (m) =>
                {
                    var import = (m.Groups[3].Value=="" ? m.Groups[4].Value : m.Groups[3].Value);
                    var mergedURL = "";
                    if (import.EndsWith(".vue"))
                    {
                        mergedURL=MergeUrl(absolutePath, import, isFolder);
                        return $"import{m.Groups[1].Value}'{(mergedURL.StartsWith('/') ? "${hosturl.origin}" : "")}{mergedURL}';";
                    }
                    else if (import.EndsWith("/"))
                    {
                        var subMatch = regImportParts.Match(m.Groups[1].Value);
                        if (subMatch.Success)
                        {
                            var sb = new StringBuilder();
                            foreach (var imp in subMatch.Groups[1].Value.Split(',').Where(i => !string.IsNullOrEmpty(i.Trim())))
                            {
                                mergedURL = MergeUrl(absolutePath, $"{import}{imp.Trim()}.vue", isFolder);
                                sb.AppendLine($"import {imp.Trim()} from '{(mergedURL.StartsWith('/') ? "${hosturl.origin}" : "")}{mergedURL}';");
                            }
                            return sb.ToString();
                        }
                        else
                            return (import.StartsWith('/') ? $"import {m.Groups[1].Value} '${{hosturl.origin}}{import}';" : m.Value);
                    }
                    else
                        return (import.StartsWith('/') ? $"import {m.Groups[1].Value} '${{hosturl.origin}}{import}';" : m.Value);
                });
                fixedContent = regInlineImport.Replace(fixedContent, (m) =>
                {
                    var url = (m.Groups[1].Value[0]=='\\' ? m.Groups[1].Value[2..^2] : m.Groups[1].Value[1..^1]);
                    var quote = (m.Groups[1].Value[0]=='\\' ? m.Groups[1].Value[..2] : m.Groups[1].Value[..1]);
                    return m.Value.Replace(m.Groups[1].Value, $"{quote}{(url.StartsWith('/') ? "${hosturl.origin}" : "")}{url[..^2]}mjs{quote}");
                });
                fixedContent = regLoadModule.Replace(fixedContent, (m) =>
                {
                    var url = (m.Groups[1].Value[0]=='\\' ? m.Groups[1].Value[2..^2] : m.Groups[1].Value[1..^1]);
                    var quote = (m.Groups[1].Value[0]=='\\' ? m.Groups[1].Value[..2] : m.Groups[1].Value[..1]);
                    return m.Value.Replace(m.Groups[1].Value, $"{quote}{(url.StartsWith('/') ? "${hosturl.origin}" : "")}{url}{quote}");
                });
                return $"cacheVueFile(`${{hosturl.origin}}{absolutePath}{Name}`,`{fixedContent}`);";
            }
        }

        public IEndpointRouteBuilder AddEndpoint(IEndpointRouteBuilder builder)
        {
            builder.MapGet($"{baseURL}/{{*{PathParameter}}}.js", (HttpContext context) => ExecuteRequestAsync(context));
            return builder;
        }

        protected override async Task<CachableResponse?> ProduceCachableResponseAsync(HttpContext context)
        {
            var spath = (string)context.Request.RouteValues[PathParameter]!;
            IEnumerable<SVueFile> files = [];
            var absolutePath = string.Concat(spath[..^(spath.EndsWith(".min") ? 4 : 0)], "/");
            var fpath = Utility.TranslatePath(fileProvider, baseURL, spath[..^(spath.EndsWith(".min") ? 4 : 0)]);
            if (fpath!=null)
                files = fileProvider.GetDirectoryContents(fpath)
                    .Where(f => f.Name.ToLower().EndsWith(".vue"))
                    .Select(f => new SVueFile(f));
            else
            {
                var name = spath[(spath.LastIndexOf('/')+1)..];
                absolutePath=spath[..(spath.LastIndexOf('/')+1)];
                fpath = Utility.TranslatePath(fileProvider, baseURL, spath[..^name.Length]);
                name = (name.EndsWith(".min.js") ? name[..^7] : name[..^3]).ToLower()+".vue";
                if (fpath!=null)
                    files = fileProvider.GetDirectoryContents(fpath)
                        .Where(f => string.Equals(f.Name, name, StringComparison.InvariantCultureIgnoreCase))
                        .Select(f => new SVueFile(f));
            }
            if (files.Any())
            {
                StringBuilder sb = new();
                sb.AppendLine(@$"import {{ loadModule }} from '{vueLoaderImportPath}';
import {{defineAsyncComponent}} from '{vueImportPath}';
import {{cacheVueFile, vueSFCOptions, addLinkedDomain}} from '{coreImport}';

{Constants.HOST_URL_CONSTRUCTOR}
addLinkedDomain(hosturl.origin);");

                var multipleFiles = files.Count()>1;

                var imports = files.SelectMany(file => file.Imports)
                    .Where(imp => imp!=vueImportPath)
                    .Select(imp => MergeUrl(absolutePath, imp, multipleFiles))
                    .Distinct();

                var importCaches = imports.Where(imp => imp.Length<=4
                        || (imp.Length>4
                            && !string.Equals(imp[^4..], ".vue", StringComparison.InvariantCultureIgnoreCase)
                            )
                        )
                    .Select(imp =>
                    {
                        if (isModelUrl(imp))
                            return $"{(!imp.EndsWith("mjs", StringComparison.InvariantCultureIgnoreCase) ? imp[..^2] : imp[..^3])}{(compressAllJS||spath.EndsWith(".min.js") ? "min." : "")}js";
                        return imp;
                    })
                    .Select(imp => $"`{(imp.StartsWith('/') ? "${hosturl.origin}" : "")}{imp}`");

                if (importCaches.Any())
                    sb.Append($"const imports = ");
                if (imports.Any())
                {
                    sb.AppendLine($@"await Promise.all([
{string.Join(",\n", importCaches.Select(c => $"import({c})"))}{(importCaches.Any()&&imports.Any(imp => imp.EndsWith(".vue", StringComparison.InvariantCulture)) ? "," : "")}
{string.Join(",\n", MergeVueImports((multipleFiles ? absolutePath.Trim('/') : baseURL),
                    imports.Where(imp => imp.EndsWith(".vue", StringComparison.InvariantCulture)), files)
            .Select(imp => $"import(`{(imp.StartsWith('/') ? "${hosturl.origin}" : "")}{imp}`)"))}
]);");
                }
                importCaches
                .ForEach((c, index) => sb.AppendLine($"vueSFCOptions.moduleCache[{c}] = {{...{{__esModule:true}}, ...imports[{index}]}};"));

                //append file content cache
                files.ForEach(file => sb.AppendLine(file.FormatCache(absolutePath, multipleFiles, isModelUrl)));

                //append module definitions
                SortFiles(files, baseURL)
                    .ForEach(file =>
                    {
                        var fileName = FormatFileName(file.Name);
                        sb.AppendLine($"const {fileName} = defineAsyncComponent(() => loadModule(`${{hosturl.origin}}{absolutePath}{file.Name}`, vueSFCOptions));");
                    });

                if (files.Count()==1)
                    sb.AppendLine($"export default {FormatFileName(files.First().Name)};");
                else
                    sb.AppendLine($"export {{{string.Join(',', files.Select(file => FormatFileName(file.Name)))}}};");

                if (sb.Length>0)
                {
                    sb.Length-=2;
                    return new CachableResponse(
                        (compressAllJS || spath.EndsWith(".min.js", StringComparison.InvariantCultureIgnoreCase) ? JSMinifier.Minify(sb.ToString()) : sb.ToString()),
                        "text/javascript",
                        files.OrderByDescending(f => f.LastModified.Ticks).Last().LastModified.DateTime,
                        files.Where(f => !string.IsNullOrEmpty(f.PhysicalPath)).Select(f => fileProvider.Watch(f.PhysicalPath!))
                        );
                }
            }
            await ReturnNotFound(context, "Unable to locate requested file.");
            return null;
        }

        private static IEnumerable<string> MergeVueImports(string baseURL, IEnumerable<string> vueImports, IEnumerable<SVueFile> files)
            => vueImports.Where(imp => !files.Any(f => FileMatch(f.Name, imp, baseURL)))
                    .Select(imp => MergeUrl(baseURL, imp, files.Count()>1))
                    .Select(url => (url.EndsWith(".js") ? url : string.Concat(url.AsSpan(0, url.Length-4), ".js")))
                    .Distinct();

        private static List<SVueFile> SortFiles(IEnumerable<SVueFile> files, string baseURL)
        {
            var list = files.ToList();
            List<SVueFile> result = new();
            result.AddRange(list.Where(f => !f.Imports.Any()).ToArray());
            list.RemoveAll(f => !f.Imports.Any());
            result.AddRange(list.Where(f => !f.Imports.Any(i => list.Any(fi => FileMatch(fi.Name, i, baseURL)))).ToArray());
            list.RemoveAll(f => !f.Imports.Any(i => list.Any(fi => FileMatch(fi.Name, i, baseURL))));
            bool changed = true;
            while (list.Count>0 && changed)
            {
                changed=false;
                for (int x = 0; x<list.Count; x++)
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

        private static bool FileMatch(string name, string import, string basePath)
            => name==import ||
            string.Equals($"./{name}", import, StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals($"{name[..name.LastIndexOf(".")]}.js", import, StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals($"./{name[..name.LastIndexOf(".")]}.js", import, StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals($"/{basePath}/{name}", import, StringComparison.InvariantCultureIgnoreCase);

        private static string MergeUrl(string baseUrl, string path, bool isFolder)
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
