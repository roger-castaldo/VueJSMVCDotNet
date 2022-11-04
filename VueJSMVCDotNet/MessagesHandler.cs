using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    internal class MessagesHandler : IDisposable
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

export default function (message,args,language) {{
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
}}";
        private static readonly string _COMPRESS_BASE_CODE_TEMPLATE = JSMinifier.Minify(_BASE_CODE_TEMPLATE);

        private struct CachedContent
        {
            private DateTime _timestamp;
            public DateTime Timestamp { get { return _timestamp; } }
            private string _content;
            public string Content { get { return _content; } }

            public CachedContent(Microsoft.Extensions.FileProviders.IDirectoryContents contents, string content)
            {
                _timestamp=new DateTime(contents
                    .OrderByDescending(f => f.LastModified)
                    .FirstOrDefault()
                    .LastModified.Ticks);
                _content = content;
            }
        }

        private readonly IFileProvider _fileProvider;
        private string _baseURL;
        private ConcurrentDictionary<string, CachedContent> _cache;


        public MessagesHandler(IFileProvider fileProvider, string baseURL)
        {
            _fileProvider=fileProvider;
            _cache = new ConcurrentDictionary<string, CachedContent>();
            _baseURL=baseURL;
        }

        public bool HandlesRequest(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments(new PathString(_baseURL))
                && context.Request.Method=="GET"
                && context.Request.Path.ToString().ToLower().EndsWith(".js");
        }

        public async Task ProcessRequest(HttpContext context)
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
                    IDirectoryContents contents = Utility.SearchPath(_fileProvider,_baseURL, spath.Substring(0, spath.Length-(spath.EndsWith(".min.js") ? 7 : 3)));
                    if (contents!=null && contents.Exists)
                    {
                        StringBuilder sb = new StringBuilder();
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
                            cc = new CachedContent(contents, sb.ToString());
                            _cache.TryAdd(spath, cc.Value);
                        }
                    }
                }
                if (cc.HasValue)
                {
                    context.Response.Headers.Add("Cache-Control", "public, must-revalidate, max-age=3600");
                    context.Response.ContentType = "text/javascript";
                    await context.Response.WriteAsync((spath.EndsWith(".min.js") ? string.Format(_COMPRESS_BASE_CODE_TEMPLATE,JSMinifier.Minify(cc.Value.Content)) : string.Format(_BASE_CODE_TEMPLATE,cc.Value.Content)));
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
            _cache.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
