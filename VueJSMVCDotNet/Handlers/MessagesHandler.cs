using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.IO;
using VueJSMVCDotNet.Caching;

namespace VueJSMVCDotNet.Handlers
{
    internal class MessagesHandler : RequestHandlerBase
    {
        private string CompileToCode(StringBuilder messages)
        {
            return $@"import {{Language}} from '{_corePath}';
import {{computed}} from '{_vuePath}';

const messages = {{
    {messages}
}};

const _format = function (str, args) {{
    if (args === undefined || args === null) {{
        return str;
    }}
    return str.replace(/{{(\d+)}}/g, function (match, number) {{
        return (typeof args[number] !== 'undefined' && args[number] == null ? '' : args[number]);
    }});
}};

const _translate = function(message,args,language){{
    let splt = message.split('.');
    let ret = null;
    let langs = [Language.value, 'en'];
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

const Translate = function(message,args) {{
    return _translate(message,args,Language);
}};

const ProduceComputedMessage = function(message,args) {{
    return computed(()=>{{
        return _translate(message,args,Language);
    }});
}};

export {{Translate,ProduceComputedMessage}};";
        }

        private readonly IFileProvider _fileProvider;
        private readonly string _baseURL;
        private readonly bool _compressAllJS;
        private readonly string _corePath;
        private readonly string _vuePath;


        public MessagesHandler(IFileProvider fileProvider, string baseURL, ILogger log,bool compressAllJS, RequestDelegate next, IMemoryCache cache, string corePath, string vuePath)
            : base(next, cache, log)
        {
            _fileProvider=fileProvider;
            _baseURL=baseURL;
            _compressAllJS=compressAllJS;
            _corePath=corePath;
            _vuePath=vuePath;
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
                cc = this[spath];
                if (cc.HasValue)
                {
                    if (context.Request.Headers.TryGetValue("If-Modified-Since", out StringValues value)
                        && cc.Value.Timestamp.ToUniversalTime().ToString("R").Equals(value.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        context.Response.ContentType="text/javascript";
                        context.Response.Headers.Add("accept-ranges", "bytes");
                        context.Response.Headers.Add("date", cc.Value.Timestamp.ToUniversalTime().ToString("R"));
                        context.Response.Headers.Add("etag", $"\"{BitConverter.ToString(System.Security.Cryptography.MD5.HashData(System.Text.ASCIIEncoding.ASCII.GetBytes(cc.Value.Timestamp.ToUniversalTime().ToString("R")))).Replace("-", "").ToLower()}\"");
                        context.Response.StatusCode = 304;
                        await context.Response.WriteAsync("");
                        respond=false;
                    }
                }
                if (respond)
                {
                    if (!cc.HasValue)
                    {
                        string fpath = Utility.TranslatePath(_fileProvider, _baseURL, spath[..^(spath.EndsWith(".min.js") ? 7 : 3)]);
                        if (fpath!=null)
                        {
                            StringBuilder sb = new();
                            IDirectoryContents contents = _fileProvider.GetDirectoryContents(fpath);
                            foreach (IFileInfo f in contents.Where(f => f.Name.ToLower().EndsWith(".json")))
                            {
                                StreamReader sr = new(f.CreateReadStream());
                                sb.AppendLine($"   {f.Name[..^5]}:{sr.ReadToEnd()},");
                                sr.Close();
                            }
                            if (sb.Length>0)
                            {
                                sb.Length-=2;
                                cc = new CachedContent(
                                    contents.OrderByDescending(ifi=>ifi.LastModified.Ticks).Last().LastModified, 
                                    (_compressAllJS ? JSMinifier.Minify(CompileToCode(sb)) : CompileToCode(sb))
                                );
                                _fileProvider.Watch(fpath).RegisterChangeCallback(state =>
                                {
                                    this[(string)state]=null;
                                }, spath);
                                this[spath] = cc.Value;
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
    }
}
