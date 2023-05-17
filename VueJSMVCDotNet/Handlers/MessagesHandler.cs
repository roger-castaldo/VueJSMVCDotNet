using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers
{
    internal class MessagesHandler : RequestHandlerBase
    {
        private const string _BASE_CODE_TEMPLATE = @"const _format = function (str, args) {{
    if (args === undefined || args === null) {{
        return str;
    }}
    return str.replace(/{{(\d+)}}/g, function (match, number) {{
        return (typeof args[number] !== 'undefined' && args[number] == null ? '' : args[number]);
    }});
}};

const messages = {{
    {0}
}};

function Translate(message,args,language) {{
    language = (language===undefined ? window.navigator.userLanguage || window.navigator.language : language);
    if (language.indexOf('-') >= 0) {{
        language = language.substring(0, language.indexOf('-'));
    }}
    let splt = message.split('.');
    let ret = null;
    let langs = [language, 'en'];
    langs.some((lang) => {{
        ret = messages[lang];
        let idx = 0;
        while (ret != undefined && ret != null) {{
            ret = ret[splt[idx]];
            idx++;
            if (idx >= splt.length) {{
                break;
            }}
        }}
        if (ret != undefined && ret != null) {{
            return true;
        }}
    }});
    return (ret == null || ret == undefined ? message : _format(ret,args));
}}

export default Translate;";

        private readonly IFileProvider _fileProvider;
        private readonly string _baseURL;
        private readonly ILogWriter _log;
        private readonly bool _compressAllJS;
        private ConcurrentDictionary<string, CachedContent> _cache;


        public MessagesHandler(IFileProvider fileProvider, string baseURL,ILogWriter log,bool compressAllJS,RequestDelegate next)
            : base(next)
        {
            _fileProvider=fileProvider;
            _baseURL=baseURL;
            _log=log;
            _compressAllJS=compressAllJS;
            _cache = new ConcurrentDictionary<string, CachedContent>();
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
                        string fpath = Utility.TranslatePath(_fileProvider, _baseURL, spath.Substring(0, spath.Length-(spath.EndsWith(".min.js") ? 7 : 3)));
                        if (fpath!=null)
                        {
                            StringBuilder sb = new StringBuilder();
                            IDirectoryContents contents = _fileProvider.GetDirectoryContents(fpath);
                            foreach (IFileInfo f in contents.Where(f => f.Name.ToLower().EndsWith(".json")))
                            {
                                StreamReader sr = new StreamReader(f.CreateReadStream());
                                sb.AppendFormat("\n\t{0}:", f.Name.Substring(0, f.Name.Length-5));
                                sb.Append(sr.ReadToEnd());
                                sr.Close();
                                sb.Append(",\n");
                            }
                            if (sb.Length>0)
                            {
                                sb.Length=sb.Length-2;
                                cc = new CachedContent(contents, (_compressAllJS ? JSMinifier.Minify(string.Format(_BASE_CODE_TEMPLATE, sb.ToString())) : string.Format(_BASE_CODE_TEMPLATE, sb.ToString())));
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

        public override void Dispose()
        {
            _cache.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
