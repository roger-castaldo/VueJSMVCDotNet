using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles
{
    internal class VueFilesHandler : IDisposable
    {
        private readonly IFileProvider fileProvider;
        private readonly string baseURL;
        private readonly ILogWriter logWriter;
        private readonly string vueImportPath;
        private ConcurrentDictionary<string, CachedContent> cache;

        public VueFilesHandler(IFileProvider fileProvider, string baseURL, ILogWriter logWriter, string vueImportPath) 
        {
            this.fileProvider=fileProvider;
            this.baseURL=baseURL;
            this.logWriter=logWriter;
            this.vueImportPath=vueImportPath;
            this.cache=new ConcurrentDictionary<string, CachedContent>();
        }

        public bool HandlesRequest(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments(new PathString(baseURL))
                && context.Request.Method=="GET"
                && context.Request.Path.ToString().ToLower().EndsWith(".js");
        }

        public async Task ProcessRequest(HttpContext context)
        {
            string spath = context.Request.Path.ToString().ToLower();
            CachedContent? cc = null;
            bool respond = true;
            if (this.cache.ContainsKey(spath))
            {
                cc = this.cache[spath];
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
            if (respond){
                if(!cc.HasValue)
                {
                    string fpath = Utility.TranslatePath(this.fileProvider, this.baseURL, spath.Substring(0, spath.Length-(spath.EndsWith(".min.js") ? 7 : 3)));
                    if (fpath!=null)
                    {
                        StringBuilder sb = new StringBuilder();
                        IDirectoryContents contents = this.fileProvider.GetDirectoryContents(fpath);
                        StringBuilder exp = new StringBuilder();
                        foreach (IFileInfo f in contents.Where(f => f.Name.ToLower().EndsWith(".vue")))
                        {
                            sb.AppendFormat("import {0} from {1}/{0}{2}.js", new object[]
                            {
                                    f.Name,
                                    spath.Substring(0, spath.Length-(spath.EndsWith(".min.js") ? 7 : 3)),
                                    spath.Contains(".min.js") ? ".min" : ""
                            });
                            exp.AppendFormat("{0},", f.Name);
                        }
                        if (exp.Length>0)
                        {
                            exp.Length=exp.Length-1;
                            sb.AppendFormat(@"export {{{1}}}", new object[]
                            {
                                    exp.ToString()
                            });
                        }
                        if (sb.Length>0)
                        {
                            cc = new CachedContent(contents, sb.ToString());
                            this.fileProvider.Watch(fpath).RegisterChangeCallback(state =>
                            {
                                CachedContent ctemp;
                                cache.TryRemove((string)state, out ctemp);
                            }, spath);
                            cache.TryAdd(spath, cc.Value);
                        }
                    }
                    else
                    {
                        fpath = Utility.TranslatePath(this.fileProvider, this.baseURL, spath.Substring(0, spath.LastIndexOf('/')));
                        if (fpath!=null)
                        {
                            string fname = spath.Substring(spath.LastIndexOf('/')+1);
                            fname=fname.Replace(".min.js", "").Replace(".js", "").ToLower()+".vue";
                            IDirectoryContents contents = this.fileProvider.GetDirectoryContents(fpath);
                            foreach (IFileInfo f in contents.Where(f => f.Name.ToLower().EndsWith(".vue")))
                            {
                                if (f.Name.ToLower()==fname)
                                {
                                    VueFileCompiler vfc = new VueFileCompiler(new StreamReader(f.CreateReadStream()), f.Name, this.vueImportPath);
                                    cc = new CachedContent(f, vfc.AsCompiled);
                                    this.fileProvider.Watch(fpath).RegisterChangeCallback(state =>
                                    {
                                        CachedContent ctemp;
                                        cache.TryRemove((string)state, out ctemp);
                                    }, spath);
                                    cache.TryAdd(spath, cc.Value);
                                    break;
                                }
                            }   
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

        public void Dispose()
        {
            cache.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
