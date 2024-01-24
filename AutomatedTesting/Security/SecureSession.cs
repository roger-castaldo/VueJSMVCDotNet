﻿using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Linq;

namespace AutomatedTesting.Security
{
    internal class SecureSession : ISecureSession,ISecureSessionFactory
    {
        private string[] _rights = null;

        public SecureSession()
        {
        }

        public SecureSession(string[] rights)
        {
            _rights = rights;
        }

        public SecureSession(ArrayList arrayList)
        {
            if (arrayList!=null)
            {
                _rights = new string[arrayList.Count];
                for(int x = 0; x<arrayList.Count; x++)
                {
                    _rights[x] = (string)arrayList[x];
                }
            }
        }

        public bool HasRight(string right)
        {
            if (_rights == null)
                return true;
            return _rights.Contains(right);
        }

        public void LinkToRequest(HttpContext context)
        {
            context.Request.Headers.Add("RIGHTS", System.Text.UTF8Encoding.UTF8.GetString(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(_rights, typeof(string[]))));
        }

        public ISecureSession ProduceFromContext(HttpContext context)
        {
            if (context.Request.Headers.ContainsKey("RIGHTS"))
                return new SecureSession((string[])System.Text.Json.JsonSerializer.Deserialize(context.Request.Headers["RIGHTS"].ToString(), typeof(string[])));
            else
                return new SecureSession();
        }
    }
}
