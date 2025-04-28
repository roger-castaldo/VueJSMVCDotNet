using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AutomatedTesting.Factory
{
    internal class HeaderCollection(HttpResponseHeaders responseHeaders, HttpContentHeaders contentHeaders)
    {
        public CacheControlHeaderValue? CacheControl => responseHeaders.CacheControl;
        public IEnumerable<string> GetValues(string name)
        {
            try
            {
                return responseHeaders.GetValues(name);
            }
            catch
            {
                return contentHeaders.GetValues(name);
            }
        }

        public bool TryGetValues(string name, out IEnumerable<string> values)
        {
            if (responseHeaders.TryGetValues(name, out values))
                return true;
            return contentHeaders.TryGetValues(name, out values);
        }
    }
}
