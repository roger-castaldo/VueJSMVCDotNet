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

        public void ProduceEvent(ref StringBuilder sb, IParsedComponent[] components, string name, ref int cacheCount, HTMLElement owner)
        {
            switch (_event)
            {
                case "click":
                case "focus":
                    sb.AppendFormat("on{0}{1}: _cache[{2}] || (_cache[{2}] = $event => (", new object[]
                    {
                        _event[0].ToString().ToUpper(),
                        _event.Substring(1),
                        cacheCount
                    });
                    break;
                default:
                    sb.AppendFormat("\"on:{0}\": _cache[{1}] || (_cache[{1}] = $event => (", new object[]
                    {
                        _event,
                        cacheCount
                    });
                    break;
            }
            sb.AppendFormat("{0}))", VueFileCompiler.ProcessClassProperties(components, _callback));
            cacheCount++;
        }
    }
}
