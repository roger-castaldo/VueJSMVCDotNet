using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens.VueDirectives
{
    internal class EventDirective : IEventDirective
    {
        string _event;
        string _callback;

        public EventDirective(string @event, string callback)
        {
            _event=@event;
            _callback=callback;
        }

        public string AsString => string.Format("v-on:{0}=\"{1}\"",new object[]
        {
            _event,
            _callback
        });

        public int Cost => 0;

        public void ProduceEvent(ref StringBuilder sb, IParsedComponent[] components, string name, ref int cacheCount, IHTMLElement owner,bool isSetup)
        {
            switch (_event)
            {
                case "click":
                case "focus":
                    sb.AppendFormat("on{0}{1}", new object[]
                    {
                        _event[0].ToString().ToUpper(),
                        _event.Substring(1)
                    });
                    break;
                default:
                    sb.AppendFormat("\"on:{0}\"", new object[]
                    {
                        _event,
                        cacheCount
                    });
                    break;
            }
            if (!isSetup)
                sb.AppendFormat(": _cache[{0}] || (_cache[{0}] = (...args) => ({1} && {1}(...args)))", new object[] { cacheCount, VueFileCompiler.ProcessClassProperties(components, _callback) });
            else
                sb.AppendFormat(": {0}", new object[] { _callback });
            cacheCount++;
        }
    }
}
