using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles
{
    internal class Handler
    {
        internal static bool IsMatch(string path)
        {
            return VirtualDirectoryInfo.WWWRoot.FindFile(path.Replace(".min", "").Replace(".js", "")+".vue").Exists;
        }

        public async Task ProcessRequest(HttpContext context)
        {
            bool min = context.Request.Path.ToString().Contains(".min.");
            VirtualFileInfo fi = VirtualDirectoryInfo.WWWRoot.FindFile(context.Request.Path.ToString().Replace(".min", "").Replace(".js", "")+".vue");
            bool transmit = true;
            StringBuilder sb = new StringBuilder();
            if (!RequestCache.CheckCache(ref context, fi.LastWriteTime))
            {
                VueFileConverter.sConvertedFile? file = VueFileConverter.ConvertFile(fi);
                if (file.HasValue)
                {
                    sb.Append("define([");
                    if (file.Value.CSS!=null)
                        sb.Append("'vue',");
                    foreach (string str in file.Value.DefineMap.Keys)
                    {
                        sb.AppendFormat("'{0}',", str);
                    }
                    foreach (string str in file.Value.VueImports.Keys)
                    {
                        string path = file.Value.VueImports[str].FullName;
                        path = path.Substring(path.IndexOf(string.Format("{0}resources{0}", Path.DirectorySeparatorChar)+1));
                        path = path.Replace(".vue", (context.Request.Path.ToString().EndsWith(".min.js") ? ".min" : ""));
                        sb.AppendFormat("'{0}',", new object[] { path });
                    }
                    foreach (string str in file.Value.Defines)
                    {
                        if (!file.Value.DefineMap.ContainsKey(str))
                        {
                            if (str.EndsWith(".vue"))
                            {
                                string path = str;
                                path = path.Substring(path.IndexOf(string.Format("{0}resources{0}", Path.DirectorySeparatorChar))+1).Replace(Path.DirectorySeparatorChar, '/');
                                path = path.Replace(".vue", (context.Request.Path.ToString().EndsWith(".min.js") ? ".min" : ""));
                                sb.AppendFormat("'{0}',", new object[] { path });
                            }
                            else
                                sb.AppendFormat("'{0}',", _CombineURI(context.Request.Path.ToString(), str));
                        }
                    }
                    if (sb.ToString().EndsWith(","))
                        sb.Length=sb.Length-1;
                    sb.Append(@"],
    function(");
                    if (file.Value.CSS!=null)
                        sb.Append("vue,");
                    foreach (string str in file.Value.DefineMap.Keys)
                        sb.AppendFormat("{0},", file.Value.DefineMap[str]);
                    foreach (string str in file.Value.VueImports.Keys)
                        sb.AppendFormat("{0},", str);
                    if (sb.ToString().EndsWith(","))
                        sb.Length=sb.Length-1;
                    sb.AppendLine("){");
                    if (file.Value.CSS!=null)
                        sb.AppendLine(string.Format("vue.style('{0}',{2},`{1}`);", new object[]{
                            GetHashString(fi.FullName),
                            file.Value.CSS,
                            file.Value.ScopedCSS.ToString().ToLower()
                        })); ;
                    sb.AppendLine(file.Value.GetAdditionalCode(min));
                    sb.AppendFormat(@"        return {0};
    }}
);", file.Value.GetDefinition(min));
                }
            }
            else
                transmit=false;
            if (sb.Length>0)
            {
                context.Response.ContentType="text/javascript";
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync(sb.ToString());
            }
            else
            {
                if (transmit)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Unable to locate requested file.");
                }
            }
        }

        private string _CombineURI(string path, string url)
        {
            if (url.StartsWith("."))
            {
                List<string> tmp = new List<string>(path.Split('/'));
                tmp.RemoveAt(tmp.Count - 1);
                while (url.StartsWith("../"))
                {
                    url = url.Substring(3);
                    tmp.RemoveAt(tmp.Count - 1);
                }
                if (url.StartsWith("./"))
                    url = url.Substring(2);
                string ret = "";
                foreach (string str in tmp)
                    ret += "/" + str;
                if (url != "")
                    ret += (url.StartsWith("/") ? "" : "/") + url;
                return ret;
            }
            return url;
        }

        public static string GetHashString(string inputString)
        {
            byte[] data;
            using (HashAlgorithm algorithm = SHA256.Create())
            {
                data = algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
            }
            return Convert.ToBase64String(data);
        }
    }
}
