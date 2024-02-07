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
            return $@"import {{Language}} from '{corePath}';
import {{computed}} from '{vuePath}';

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

        private readonly IFileProvider fileProvider;
        private readonly string baseURL;
        private readonly bool compressAllJS;
        private readonly string corePath;
        private readonly string vuePath;


        public MessagesHandler(IFileProvider fileProvider, string baseURL, ILogger log,bool compressAllJS, RequestDelegate next, IMemoryCache cache, string corePath, string vuePath)
            : base(next, cache, log)
        {
            this.fileProvider=fileProvider;
            this.baseURL=baseURL;
            this.compressAllJS=compressAllJS;
            this.corePath=corePath;
            this.vuePath=vuePath;
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments(new PathString(baseURL))
                && context.Request.Method=="GET"
                && context.Request.Path.ToString().ToLower().EndsWith(".js"))
            {
                string spath = context.Request.Path.ToString().ToLower();
                CachedContent cc = null;
                cc = this[spath];
                if (!await ReponseCached(context, cc))
                {
                    if (cc==null)
                    {
                        string fpath = Utility.TranslatePath(fileProvider, baseURL, spath[..^(spath.EndsWith(".min.js",StringComparison.InvariantCultureIgnoreCase) ? 7 : 3)]);
                        if (fpath!=null)
                        {
                            StringBuilder sb = new();
                            var contents = fileProvider.GetDirectoryContents(fpath)
                                .Where(f => f.Name.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase));
                                
                            contents.ForEach(f =>
                                {
                                    StreamReader sr = new(f.CreateReadStream());
                                    sb.AppendLine($"   {f.Name[..^5]}:{sr.ReadToEnd()},");
                                    sr.Close();
                                });
                            if (sb.Length>0)
                            {
                                sb.Length-=2;
                                cc = new()
                                {
                                    Timestamp=contents.OrderByDescending(ifi => ifi.LastModified.Ticks).Last().LastModified.DateTime,
                                    Content=(compressAllJS ? JSMinifier.Minify(CompileToCode(sb)) : CompileToCode(sb))
                                };
                                fileProvider.Watch($"{fpath}{Path.DirectorySeparatorChar}*.json").RegisterChangeCallback(state =>
                                {
                                    this[(string)state]=null;
                                }, spath);
                                this[spath] = cc;
                            }
                        }
                    }
                    if (cc!=null)
                        await ProduceResponse(context, "text/javascript", cc.Timestamp, (!compressAllJS && spath.EndsWith(".min.js") ? JSMinifier.Minify(cc.Content) : cc.Content));
                    else
                        await ProduceNotFound(context, "Unable to locate requested file.");
                }
            }
            else
                await next(context);
        }
    }
}
