using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers
{
    internal class VueFilesHandler : RequestHandlerBase
    {

        private static readonly Regex _regImport = new Regex(@"^\s*import (.+) from (""[^""]+""|'[^']+')", RegexOptions.Multiline);

        private struct sVueFile
        {
            private string _name;
            public string Name => _name;
            private string _content;
            public string Content => _content;

            public sVueFile(IFileInfo f)
            {
                _name=f.Name;
                StreamReader sr = new StreamReader(f.CreateReadStream());
                _content=sr.ReadToEnd().Replace("`", "\\`");
                sr.Close();
            }

            public string[] Imports
            {
                get
                {
                    List<string> ret = new List<string>();
                    foreach (Match m in _regImport.Matches(_content))
                        ret.Add(m.Groups[2].Value);
                    return ret.ToArray();
                }
            }

            internal string FormatCase(Dictionary<string, string> importMaps)
            {
                return string.Format(@"          case '{0}':
                return Promise.resolve(`{1}`);
                break;", new object[]
                {
                    Name,
                    _regImport.Replace(_content, (m) => string.Format("import {0} from '{1}'", new object[]
                    {
                        m.Groups[1].Value,
                        importMaps[m.Groups[2].Value]
                    }))
                });
            }
        }

        private readonly IFileProvider _fileProvider;
        private readonly string _baseURL;
        private readonly ILogWriter _log;
        private readonly string _vueImportPath;
        private readonly string _vueLoaderImportPath;
        private ConcurrentDictionary<string, CachedContent> _cache;

        public VueFilesHandler(IFileProvider fileProvider, string baseURL, ILogWriter log,string vueImportPath, string vueLoaderImportPath,RequestDelegate next)
            : base(next) 
        {
            _fileProvider=fileProvider;
            _baseURL=baseURL;
            _log=log;
            _cache=new ConcurrentDictionary<string, CachedContent>();
            _vueImportPath=vueImportPath;
            _vueLoaderImportPath=vueLoaderImportPath;
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments(new PathString(_baseURL))
                && context.Request.Method=="GET"
                && context.Request.Path.ToString().ToLower().EndsWith(".js"))
            {
                string spath = context.Request.Path.ToString().ToLower();
                CachedContent? cc = null;
                bool respond = true;
                if (_cache.ContainsKey(spath))
                {
                    cc = _cache[spath];
                    if (context.Request.Headers.ContainsKey("If-Modified-Since"))
                    {
                        if (cc.Value.Timestamp.ToUniversalTime().ToString("R").ToLower()==context.Request.Headers["If-Modified-Since"].ToString().ToLower())
                        {
                            context.Response.ContentType="text/javascript";
                            context.Response.Headers.Add("accept-ranges", "bytes");
                            context.Response.Headers.Add("date", cc.Value.Timestamp.ToUniversalTime().ToString("R"));
                            context.Response.Headers.Add("etag", string.Format("\"{0}\"", BitConverter.ToString(System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(cc.Value.Timestamp.ToUniversalTime().ToString("R")))).Replace("-", "").ToLower()));
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
                            int idx = 0;
                            Dictionary<string, string> importMaps = new Dictionary<string, string>()
                        {
                            {string.Format("'{0}'",_vueImportPath),"vue" }
                        };
                            foreach (sVueFile file in files)
                            {
                                foreach (string str in file.Imports)
                                {
                                    if (!importMaps.ContainsKey(str))
                                    {
                                        importMaps.Add(str, string.Format("mod{0}", idx));
                                        idx++;
                                    }
                                }
                            }

                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine(string.Format("import {{ loadModule,createCJSModule }} from '{0}';", _vueLoaderImportPath));
                            foreach (string str in importMaps.Keys)
                                sb.AppendLine(string.Format("import * as {0} from {1}", new object[] { importMaps[str], str+(str.Trim().EndsWith(";") ? "" : ";") }));

                            sb.AppendLine(@"
const options = {
    addStyle: () => {},
    moduleCache: {");
                            foreach (string str in importMaps.Keys)
                                sb.AppendLine(string.Format("\t\t\t{0} : {0},", importMaps[str]));
                            if (importMaps.Count>0)
                                sb.Length-=3;

                            sb.AppendLine(@"        },
    getFile: async (url) => { 
        switch(url) {");
                            foreach (sVueFile file in files)
                                sb.AppendLine(file.FormatCase(importMaps));
                            sb.AppendLine(@"
            default:
                if (url.endsWith('.vue')||url.endsWith('.mjs')){
                    url = url.substring(0,url.length-4)+'.js';
                }
                const res = await fetch(url);
                if ( !res.ok )
                    throw Object.assign(new Error(url+' '+res.statusText), { res });
                return await res.text();
                break;
        }
    }
};");
                            foreach (sVueFile file in files)
                                sb.AppendLine(string.Format("const {0} = vue.defineAsyncComponent(() => loadModule('{1}', options));", new object[]
                                {
                                _FormatFileName(file.Name),
                                file.Name
                                }));
                            if (files.Count==1)
                                sb.AppendLine(string.Format("export default {0};", _FormatFileName(files[0].Name)));
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
                                cc = new CachedContent(contents, sb.ToString());
                                _fileProvider.Watch(fpath).RegisterChangeCallback(state =>
                                {
                                    CachedContent ctemp;
                                    _cache.TryRemove((string)state, out ctemp);
                                }, spath);
                                _cache.TryAdd(spath, cc.Value);
                            }
                        }
                    }
                    if (cc.HasValue)
                    {
                        context.Response.Headers.Add("Cache-Control", "public, must-revalidate, max-age=3600");
                        context.Response.ContentType = "text/javascript";
                        await context.Response.WriteAsync((spath.EndsWith(".min.js") ? JSMinifier.Minify(cc.Value.Content) : cc.Value.Content));
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

        private static string _FormatFileName(string fileName)
        {
            return fileName.Substring(0, fileName.Length-4).Replace(".", "_");
        }

        public override void Dispose()
        {
            _cache.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
