using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VueJSMVCDotNet.Caching
{
    internal struct CachedContent
    {
        private DateTime _timestamp;
        public DateTime Timestamp { get { return _timestamp; } }
        private string _content;
        public string Content { get { return _content; } }

        public CachedContent(DateTimeOffset lastModified, string content)
        {
            _timestamp = lastModified.LocalDateTime;
            _content = content;
        }
    }
}
