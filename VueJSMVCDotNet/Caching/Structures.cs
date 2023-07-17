using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VueJSMVCDotNet.Caching
{
    internal readonly struct CachedContent
    {
        public DateTime Timestamp { get; private init; }
        public string Content { get; private init; }

        public CachedContent(DateTimeOffset lastModified, string content)
        {
            Timestamp = lastModified.LocalDateTime;
            Content = content;
        }
    }
}
