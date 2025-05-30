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
    internal class MessagesEndpoint(IFileProvider fileProvider, string baseURL, bool compressAllJS, string corePath, string vuePath, ILogger? logger, IMemoryCache? cache)
        : ACachingEndpoint(logger, cache)
    {
        private const string PathParameter = "path";
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
    if (message===null) {{ return null; }}
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
                var fpath = Utility.TranslatePath(fileProvider, spath[..^(spath.EndsWith(".min.js", StringComparison.InvariantCultureIgnoreCase) ? 7 : 3)]);
                if (!string.IsNullOrWhiteSpace(fpath))
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
                        return new CachableResponse(
                            (compressAllJS || spath.EndsWith(".min.js", StringComparison.InvariantCultureIgnoreCase) ? JSMinifier.Minify(CompileToCode(sb)) : CompileToCode(sb)),
                            "text/javascript",
                            contents.OrderByDescending(ifi => ifi.LastModified.Ticks).Last().LastModified.DateTime,
                            contents.Where(f => f.PhysicalPath!=null).Select(f => fileProvider.Watch(f.PhysicalPath!))
                        );
                    }
                }
            }
            await ReturnNotFound(context, "Unable to locate requested file.");
            return null;
        }
    }
}
