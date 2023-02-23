using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.Interfaces
{
    public interface IRequestData
    {
        IEnumerable<string> Keys { get; }
        T GetValue<T>(string key);
        ISecureSession Session { get; }
    }
}
