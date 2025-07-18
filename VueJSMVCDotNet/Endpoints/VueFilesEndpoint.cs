using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using VueJSMVCDotNet.Caching;
using VueJSMVCDotNet.Javascript;

namespace VueJSMVCDotNet.Endpoints
{
    internal class VueFilesEndpoint(IFileProvider fileProvider, string baseURL, bool compressAllJS, ILogger? logger, IMemoryCache? cache)
        : ACachingEndpoint(logger, cache)
    {
        private const string PathParameter = "path";

        private static readonly Regex regImport = new(@"^\s*import\s*([^""']+)\s*from\s*(""([^""]+)""|'([^']+)');?\s*$", RegexOptions.Multiline|RegexOptions.Compiled, TimeSpan.FromHours(5));
        private static readonly Regex regInlineImport = new(@"\s*import\((""([^""]+)""|'([^']+)')\)", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

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

            private static (string mergedURL,bool remove) CorrectImportURL(string import, string absolutePath,bool isFolder,bool useMin)
            {
                var mergedURL = "";
                if (import.EndsWith(".vue", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (((import.StartsWith("./") && !import[2..].Contains('/'))||!import.Contains('/')) && isFolder)
                        return ("", true);
                    mergedURL=$"{MergeUrl(absolutePath, (import.StartsWith('/') || import.StartsWith("./") || import.StartsWith("../") ? import : $"./{import}"), isFolder)[..^4]}{(useMin ? ".min" : "")}.js";
                }
                else if (import.EndsWith('/'))
                    mergedURL=$"{MergeUrl(absolutePath, import[..^1], isFolder)}{(useMin ? ".min" : "")}.js";
                else if (import.StartsWith('.'))
                    mergedURL = MergeUrl(absolutePath, import, isFolder);
                else if (import.StartsWith('/'))
                    mergedURL = import;
                return (mergedURL,false);
            }

            public string FormatCache(string absolutePath, bool isFolder, bool useMin)
            {
                var fixedContent = regImport.Replace(content, (m) =>
                {
                    var (mergedURL,remove) = CorrectImportURL((m.Groups[3].Value=="" ? m.Groups[4].Value : m.Groups[3].Value),absolutePath,isFolder, useMin);
                    if (remove)
                        return "";
                    else if (string.IsNullOrEmpty(mergedURL))
                        return m.Value;
                    else if (mergedURL.StartsWith('/'))
                        return $"const {m.Groups[1].Value} = await import(`${{hosturl.origin}}{mergedURL}`);";
                    return $"import {m.Groups[1].Value} from '{mergedURL}';";
                });
                fixedContent = regInlineImport.Replace(fixedContent, (m) =>
                {
                    var (mergedURL,remove) = CorrectImportURL((m.Groups[2].Value=="" ? m.Groups[3].Value : m.Groups[2].Value), absolutePath, isFolder, useMin);
                    if (remove)
                        return "";
                    else if (mergedURL.StartsWith('/'))
                        return m.Value.Replace(m.Groups[1].Value, $"`${{hosturl.origin}}{mergedURL}`");
                    return m.Value;
                });
                return fixedContent;
            }
        }

        public IEndpointRouteBuilder AddEndpoint(IEndpointRouteBuilder builder)
        {
            builder.MapGet($"{baseURL}/{{**{PathParameter}}}", (HttpContext context) => ExecuteRequestAsync(context));
            return builder;
        }

        protected override async Task<CachableResponse?> ProduceCachableResponseAsync(HttpContext context)
        {
            var spath = $"{baseURL}/{context.Request.RouteValues[PathParameter]!}";
            if (spath.EndsWith(".js", StringComparison.InvariantCultureIgnoreCase))
            {
                IEnumerable<SVueFile> files = [];
                var absolutePath = string.Concat(spath[..^(spath.EndsWith(".min.js", StringComparison.InvariantCultureIgnoreCase) ? 7 : 3)], "/");
                var fpath = Utility.TranslatePath(fileProvider, spath[..^(spath.EndsWith(".min.js", StringComparison.InvariantCultureIgnoreCase) ? 7 : 3)]);
                if (fpath!=null)
                    files = fileProvider.GetDirectoryContents(fpath)
                        .Where(f => f.Name.ToLower().EndsWith(".vue"))
                        .Select(f => new SVueFile(f));
                else
                {
                    var name = spath[(spath.LastIndexOf('/')+1)..];
                    absolutePath=spath[..(spath.LastIndexOf('/')+1)];
                    fpath = Utility.TranslatePath(fileProvider, spath[..^name.Length]);
                    name = (name.EndsWith(".min.js") ? name[..^7] : name[..^3]).ToLower()+".vue";
                    if (fpath!=null)
                        files = fileProvider.GetDirectoryContents(fpath)
                            .Where(f => string.Equals(f.Name, name, StringComparison.InvariantCultureIgnoreCase))
                            .Select(f => new SVueFile(f));
                }
                if (files.Any())
                {
                    var vueFiles = files.Select(f => new VueFile(FormatFileName(f.Name), f.Name, f.FormatCache(absolutePath, files.Count()>1, compressAllJS||spath.EndsWith(".min.js"))));


                    return new CachableResponse(
                        await context.RequestServices.GetRequiredService<JSEngine>().CompileVueFilesAsync(vueFiles, (compressAllJS || spath.EndsWith(".min.js", StringComparison.InvariantCultureIgnoreCase)), Constants.HOST_URL_CONSTRUCTOR),
                        "text/javascript",
                        files.OrderByDescending(f => f.LastModified.Ticks).Last().LastModified.DateTime,
                        files.Where(f => !string.IsNullOrEmpty(f.PhysicalPath)).Select(f => fileProvider.Watch(f.PhysicalPath!))
                        );
                }
            }
            await ReturnNotFound(context, "Unable to locate requested file.");
            return null;
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
