﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    internal struct CachedContent
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

        public CachedContent(Microsoft.Extensions.FileProviders.IFileInfo fileInfo, string content)
        {
            _timestamp=new DateTime(fileInfo.LastModified.Ticks);
            _content = content;
        }
    }
}
