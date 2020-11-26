using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApplication.Handlers
{
    public class VueComponentsHandler
    {
        DirectoryInfo _webPath;

        private string _ReadFile(DirectoryInfo di,FileInfo fi)
        {
            string ret = "";
            StreamReader sr = new StreamReader(fi.OpenRead());
            ret = sr.ReadToEnd().Trim();
            sr.Close();
            ret = ret.Replace("$template$", string.Format("templates['{0}']", fi.Name.Substring(0, fi.Name.LastIndexOf('.')).Replace(".", "-").Replace(" ", "-")));
            ret = ret.Replace("$componentname$", string.Format(@"{0}-{1}", new object[] { di.Name, fi.Name.Substring(0, fi.Name.LastIndexOf('.')).Replace(".", "-").Replace(" ", "-") }));
            return ret;
        }

        public VueComponentsHandler(IHostingEnvironment env)
        {
            _webPath = new DirectoryInfo(env.WebRootPath);
        }

        public async Task ProcessRequest(HttpContext context)
        {
            DirectoryInfo di = new DirectoryInfo(_webPath.FullName + context.Request.Path.ToString().Substring(0, context.Request.Path.ToString().Length-(context.Request.Path.ToString().EndsWith(".min.js") ? 7 : 3)));
            if (di.Exists) {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("var templates={};");
                foreach (FileInfo fi in di.GetFiles("*.template"))
                {
                    sb.Append(string.Format("templates['{0}']='", fi.Name.Substring(0, fi.Name.LastIndexOf('.')).Replace(".", "-").Replace(" ", "-")));
                    string cont = _ReadFile(di,fi);
                    cont = cont.Replace("'", "\\'").Replace("\r\n", "\\\n");
                    sb.Append(cont);
                    sb.AppendLine("';");
                }
                foreach (FileInfo fi in di.GetFiles("*.json"))
                {
                    sb.AppendLine(string.Format(@"Vue.component('{0}-{1}',", new object[] { di.Name, fi.Name.Substring(0,fi.Name.LastIndexOf('.')).Replace(".","-").Replace(" ", "-") }));
                    sb.AppendLine(_ReadFile(di,fi));
                    sb.AppendLine(");");
                }
                foreach (FileInfo fi in di.GetFiles("*.js"))
                    sb.AppendLine(_ReadFile(di,fi));
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/javascript";
                await context.Response.WriteAsync(sb.ToString());
            }
            else
            {
                context.Response.StatusCode = 404;
                context.Response.ContentType = "text/text";
                await context.Response.WriteAsync("Not Found");
            }
        }
    }
}
