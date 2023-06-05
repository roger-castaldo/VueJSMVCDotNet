using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    public interface IDataStore
    {
        object this[string key] { get; set; }
    }
}
