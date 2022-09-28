using AutomatedTesting.Models;
using AutomatedTesting.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.Reddragonit.VueJSMVCDotNet;
using System;
using System.IO;

namespace AutomatedTesting
{
    internal static class Utility
    {

        public static MemoryStream ExecuteRequest(string method,string path, RequestHandler handler,out int responseStatus,SecureSession session=null)
        {
            MemoryStream ms = new MemoryStream();
            HttpContext context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Host = new HostString("localhost");
            context.Request.IsHttps = false;
            context.Request.Path = new PathString(path);
            context.Response.Body = ms;

            Assert.IsTrue(handler.HandlesRequest(context));
            handler.ProcessRequest(context,(session==null ? new SecureSession() : session)).Wait();
            responseStatus = context.Response.StatusCode;
            ms.Position = 0;
            return ms;
        }

        public static object ReadJSONResponse(MemoryStream ms)
        {
            string content = new StreamReader(ms).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            return JSON.JsonDecode(content);
        }

    }
}
