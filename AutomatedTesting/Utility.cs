using AutomatedTesting.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.Reddragonit.VueJSMVCDotNet;
using System.IO;

namespace AutomatedTesting
{
    internal static class Utility
    {

        public static MemoryStream ExecuteGet(string path, RequestHandler handler)
        {
            MemoryStream ms = new MemoryStream();
            HttpContext context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Host = new HostString("localhost");
            context.Request.IsHttps = false;
            context.Request.Path = new PathString(path);
            context.Response.Body = ms;

            Assert.IsTrue(handler.HandlesRequest(context));
            handler.ProcessRequest(context, null).Wait();
            ms.Position = 0;
            return ms;
        }

    }
}
