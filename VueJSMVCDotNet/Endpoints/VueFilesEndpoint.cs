using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.IO;
using VueJSMVCDotNet.Caching;
using VueJSMVCDotNet.Javascript;

namespace VueJSMVCDotNet.Endpoints
{
    internal class VueFilesEndpoint(IFileProvider fileProvider, string baseURL, string vueImportPath, bool compressAllJS,JSEngine? engine, ILogger? logger, IMemoryCache? cache)
        : AJSEngineEndpoint(engine,logger, cache)
    {
        private const string PathParameter = "path";

        private static readonly TimeSpan regexTimespan = TimeSpan.FromSeconds(5);

        private static readonly Regex regImport = new(@"^\s*import\s*([^""']+)\s*from\s*(""([^""]+)""|'([^']+)');?\s*$", RegexOptions.Multiline|RegexOptions.Compiled, regexTimespan);
        private static readonly Regex regInlineImport = new(@"\s*import\((""([^""]+)""|'([^']+)')\)", RegexOptions.Compiled, regexTimespan);
        private static readonly Regex regSpecialImport = new(@"^\s*const\s*(.+)\s*=\s*await\s+import\(`\$\{hosturl.origin\}(.+)`\);?\s*$", RegexOptions.Multiline|RegexOptions.Compiled, regexTimespan);
        private static readonly Regex regTemplateContextCache = new(@"\b(_ctx|_cache)\.", RegexOptions.Compiled, regexTimespan);
        private static readonly Regex regAsyncImport = new(@"^\s*const\s*(.+)\s*=\s*await\s+import\((.+)\);?\s*$",RegexOptions.Multiline|RegexOptions.Compiled, regexTimespan);
        private static readonly Regex regHoistedConstant = new(@"^const\s+(_hoisted_\d+).+$",RegexOptions.Compiled,regexTimespan);
        private static readonly Regex regResolveComponent = new(@"^\s*const\s+([^\s]+)\s*=\s*_resolveComponent\(""([^""]+)""\);?\s*$", RegexOptions.Compiled|RegexOptions.Multiline, regexTimespan);
        private static readonly Regex regInvalidNameChars = new(@"(\s|-)", RegexOptions.Compiled, regexTimespan);

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
                content=sr.ReadToEnd();
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

            public (string fixedContent, IEnumerable<string> specialImports) FormatCache(string absolutePath, bool isFolder, bool useMin)
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
                var tmpImports = new List<string>();
                fixedContent = regSpecialImport.Replace(fixedContent, (m) =>
                {
                    tmpImports.Add(m.Value);
                    return "";
                });
                return (fixedContent,tmpImports);
            }
        }

        private record ScriptImport(IEnumerable<string> Variables,IEnumerable<string> Imports,bool IsAsync);

        private record CompiledVueFile(string ID,string Name,string StyleCode,string ScriptContent, Dictionary<string,ScriptImport> Imports,IEnumerable<string> Constants);

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
                    var isMin = compressAllJS||spath.EndsWith(".min.js");
                    var jsEngine = GetEngine(context);
                    var vueFiles = files.Select(f => {
                        var (fixedContent, specialImports)= f.FormatCache(absolutePath, files.Count()>1, isMin);
                        return new VueFile(FormatFileName(f.Name), f.Name, fixedContent, specialImports);
                    });

                    var compiledFiles = await jsEngine.CompileVueFilesAsync(vueFiles);

                    var compiledVueFiles = compiledFiles.Select((f,index) => ProcessCompiledFile(f, vueFiles.ElementAt(index),index));

                    (compiledVueFiles, var mergedImports) = MergeImports(compiledVueFiles);

                    if (!mergedImports.TryGetValue(vueImportPath, out _) 
                        && mergedImports.TryGetValue("vue",out var vueImport))
                    {
                        mergedImports.Add(vueImportPath,vueImport);
                        mergedImports.Remove("vue");
                    }

                    var resultBuilder = new StringBuilder();
                    foreach(var pair in mergedImports.Where(p => !p.Value.IsAsync))
                    {
                        resultBuilder.Append("import ");
                        if (pair.Value.Variables.Any())
                            resultBuilder.Append($"{string.Join(',', pair.Value.Variables)}{(pair.Value.Imports.Any() ? "," : "")}");
                        if (pair.Value.Imports.Any())
                            resultBuilder.Append($"{{{string.Join(',', pair.Value.Imports)}}}");
                        resultBuilder.AppendLine($" from '{pair.Key}';");
                    }

                    resultBuilder.AppendLine(Constants.HOST_URL_CONSTRUCTOR);

                    foreach (var pair in mergedImports.Where(p => p.Value.IsAsync))
                    {
                        resultBuilder.Append("const {");
                        if (pair.Value.Variables.Any())
                            resultBuilder.Append($"{string.Join(',', pair.Value.Variables.Select(i => $"default: {i}"))}{(pair.Value.Imports.Any() ? "," : "")}");
                        if (pair.Value.Imports.Any())
                            resultBuilder.Append(string.Join(',', pair.Value.Imports));
                        resultBuilder.AppendLine($"}} = await import({pair.Key});");
                    }

                    resultBuilder.AppendLine(string.Join('\n', compiledVueFiles.SelectMany(cvf => cvf.Constants)));

                    foreach(var cvf in compiledVueFiles)
                    {
                        if (!string.IsNullOrWhiteSpace(cvf.StyleCode))
                        {
                            resultBuilder.AppendLine($@"(function(){{
            let styleTag = document.createElement('style');
            styleTag.setAttribute('data-v-{cvf.ID}', '');
            styleTag.innerHTML = `{cvf.StyleCode}`;
            document.head.appendChild(styleTag);
        }})();");
                        }
                        resultBuilder.AppendLine($@" var {cvf.ID} = {defineComponent}({{
                __name: '{cvf.ID}',
                {cvf.ScriptContent}
            );");
                    }

                    if (compiledVueFiles.Count()>1)
                        resultBuilder.AppendLine($"export {{{string.Join(',', compiledVueFiles.Select(cvf =>
                        {
                            if (!string.Equals(cvf.Name, $"{cvf.ID}.vue", StringComparison.InvariantCultureIgnoreCase))
                                return $"{cvf.ID}, {cvf.ID} as {regInvalidNameChars.Replace(cvf.Name.Replace(".vue",""), "_")}";
                            return cvf.ID;
                        }))}}};");
                    else
                        resultBuilder.AppendLine($"export default {compiledVueFiles.First().ID};");

                    return new CachableResponse(
                        (isMin ? await jsEngine.CompressCodeAsync(resultBuilder.ToString()) : resultBuilder.ToString()),
                        "text/javascript",
                        files.OrderByDescending(f => f.LastModified.Ticks).Last().LastModified.DateTime,
                        files.Where(f => !string.IsNullOrEmpty(f.PhysicalPath)).Select(f => fileProvider.Watch(f.PhysicalPath!))
                    );
                }
            }
            await ReturnNotFound(context, "Unable to locate requested file.");
            return null;
        }

        private const string templateExportMark = "return (_openBlock(),";
        private const string exportedMark = "export default {";
        private const string defineComponent = "defineComponent";

        private static CompiledVueFile ProcessCompiledFile(JSEngine.CompileResult compiledFile,VueFile vueFile,int index)
        {
            var scriptContent = $@"{string.Join('\n',vueFile.SpecialImports)}
{compiledFile.Script.Trim()}";
            if (!string.IsNullOrEmpty(compiledFile.TemplateScript))
            {
                var templateImports = compiledFile.TemplateScript[..compiledFile.TemplateScript.IndexOf(templateExportMark)]
                    .Replace("export function render(_ctx, _cache) {","")
                    .Trim();
                var templateRenderCode = regTemplateContextCache.Replace(
                    $"render:(h) => {{ return {compiledFile.TemplateScript[(compiledFile.TemplateScript.IndexOf(templateExportMark)+templateExportMark.Length)..].Trim()}",
                    "h."
                )[..^3];
                scriptContent = $@"{templateImports}
{scriptContent.Replace(exportedMark,$"{exportedMark}{templateRenderCode};}},")}".Trim()
.TrimEnd(';');
                
            }

            var (source, imports, constants) = ProcessImports(index, scriptContent);

            return new CompiledVueFile(vueFile.ID,vueFile.Name,compiledFile.StyleScript,source,imports, constants);
        }

        private static (string scriptSource,Dictionary<string,ScriptImport> imports,IEnumerable<string> constants) ProcessImports(int index, string scriptContent)
        {
            var scriptImports = new Dictionary<string,ScriptImport>();
            var constants = new List<string>();
            var scriptSource = regImport.Replace(scriptContent, (match) => 
            {
                scriptImports = ProcessImportRegex(scriptImports, (string.IsNullOrWhiteSpace(match.Groups[3].Value) ? match.Groups[4].Value : match.Groups[3].Value), match.Groups[1].Value.Trim(), false);
                return "";
            });
            scriptSource = regAsyncImport.Replace(scriptSource, (match) =>
            {
                scriptImports = ProcessImportRegex(scriptImports, match.Groups[2].Value.Trim(), match.Groups[1].Value.Trim(), true);
                return "";
            });
            var curConstant = "";
            foreach (var line in scriptSource[..scriptSource.IndexOf(exportedMark)].Trim().Split('\n'))
            {
                var l = line.Trim();
                if (string.IsNullOrWhiteSpace(l)&&!string.IsNullOrWhiteSpace(curConstant))
                {
                    constants.Add($"{curConstant};");
                    curConstant="";
                }
                else if (regHoistedConstant.IsMatch(l))
                {
                    if (!string.IsNullOrWhiteSpace(curConstant))
                        constants.Add($"{curConstant};");
                    var match = regHoistedConstant.Match(l);
                    var regex = new Regex($"\\b{match.Groups[1].Value}\\b");
                    scriptSource = regex.Replace(scriptSource, $"_{index}{match.Groups[1].Value}");
                    curConstant = l.Replace(match.Groups[1].Value, $"_{index}{match.Groups[1].Value}");
                }
                else if (!string.IsNullOrWhiteSpace(curConstant))
                    curConstant+=$" ${l}";
            }
            if (!string.IsNullOrWhiteSpace(curConstant))
                constants.Add(curConstant);
            scriptSource = scriptSource[(scriptSource.IndexOf(exportedMark)+exportedMark.Length)..];
            scriptSource = regResolveComponent.Replace(scriptSource, (match) =>
            {
                var result = new StringBuilder();
                result.Append($"const {match.Groups[1].Value} = ({match.Groups[2].Value}!==undefined ? {match.Groups[2].Value}.default : null)??{match.Groups[2].Value}??");
                if (!string.Equals(match.Groups[2].Value.ToLower(), match.Groups[2].Value))
                    result.Append($"({match.Groups[2].Value.ToLower()}!==undefined ? {match.Groups[2].Value.ToLower()}.default : null)??{match.Groups[2].Value.ToLower()}??");
                result.Append($"_resolveComponent(\"{match.Groups[2].Value}\");");
                return result.ToString();
            });

            return (scriptSource, scriptImports, constants);
        }

        private static (IEnumerable<CompiledVueFile> compiledFiles, Dictionary<string, ScriptImport> mergedImports) MergeImports(IEnumerable<CompiledVueFile> compiledFiles)
        {
            var curId = "aa";
            var resultFiles = new List<CompiledVueFile>();
            var mergedImports = new Dictionary<string, ScriptImport>()
            {
                { "vue",new([],[defineComponent],false) }
            };
            var currentImports = new List<string>([defineComponent]);
            foreach(var cf in compiledFiles)
            {
                var imports = cf.Imports;
                var scriptContent = cf.ScriptContent;
                foreach(var prop in imports.Keys)
                {
                    if (!mergedImports.TryGetValue(prop,out var import))
                    {
                        (scriptContent, curId, currentImports, var variables) = ProcessImportValues(scriptContent, curId, currentImports, imports[prop].Variables, []);
                        (scriptContent, curId, currentImports, var mappedImports) = ProcessImportValues(scriptContent, curId, currentImports, imports[prop].Imports, []);
                        mergedImports.Add(prop, new(variables, mappedImports, imports[prop].IsAsync));
                    }
                    else
                    {
                        mergedImports.Remove(prop);
                        (scriptContent, curId, currentImports, var variables) = ProcessImportValues(scriptContent, curId, currentImports, imports[prop].Variables, import.Variables);
                        (scriptContent, curId, currentImports, var mappedImports) = ProcessImportValues(scriptContent, curId, currentImports, imports[prop].Imports, import.Imports);
                        mergedImports.Add(prop, new(variables, mappedImports, imports[prop].IsAsync||import.IsAsync));
                    }
                }
                resultFiles.Add(new(cf.ID, cf.Name, cf.StyleCode, scriptContent, [], cf.Constants));
            }
            return (resultFiles, mergedImports);
        }

        private static (string scriptContent, string curId, List<string> currentImports, string[] resultValues) ProcessImportValues(string scriptContent, string curId,List<string> currentImports, IEnumerable<string> values, IEnumerable<string> preValues)
        {
            var resultValues = values.Except(preValues)
                .Select(i =>
                {
                    var checkValue = (i.Contains(" as ") ? i[..(i.IndexOf(" as ")+4)] : i);
                    if (!currentImports.Contains(checkValue))
                        currentImports.Add(checkValue);
                    else
                    {
                        i = $"{(i.Contains(" as ") ? i[..i.IndexOf(" as ")] : i)} as {curId}";
                        var regex = new Regex($"\\b{checkValue}\\b");
                        scriptContent = regex.Replace(scriptContent, curId);
                        curId = IncrementMapName(curId);
                    }
                    return i;
                })
                .Concat(preValues)
                .ToArray();
            return (scriptContent,curId,currentImports,resultValues);
        }

        private static string IncrementMapName(string id)
        {
            var chars = id.ToCharArray();
            var carry = 1;

            for (var i = chars.Length - 1; i >= 0; i--)
            {
                if (carry == 0) break;

                var code = (int)chars[i] - 97;
                code += carry;
                if (code>=26)
                {
                    chars[i] = 'a';
                    carry=1;
                }
                else
                {
                    chars[i] = (char)(97+code);
                    carry=0;
                }
            }

            if (carry > 0)
                chars = chars.AsEnumerable().Prepend('a').ToArray();

            return new string(chars);
        }

        private static Dictionary<string, ScriptImport> ProcessImportRegex(Dictionary<string, ScriptImport> scriptImports,string path, string importValues,bool isAsync)
        {
            IEnumerable<string> variables = [];
            IEnumerable<string> imports = [];
            if (scriptImports.TryGetValue(path, out var scriptImport))
            {
                variables = scriptImport.Variables;
                imports = scriptImport.Imports;
                isAsync |= scriptImport.IsAsync;
                scriptImports.Remove(path);
            }
            if (importValues.StartsWith('{'))
            {
                foreach (var rawImp in importValues.Split(','))
                {
                    var imp = rawImp.Trim()
                    .TrimStart('{')
                    .TrimEnd('}')
                    .Trim();
                    if (!imports.Contains(imp))
                        imports = imports.Append(imp);
                }
            }
            else
            {
                if (!variables.Contains(importValues.Trim()))
                    variables = variables.Append(importValues.Trim());
            }
            scriptImports.Add(path, new(variables, imports, isAsync));
            return scriptImports;
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
