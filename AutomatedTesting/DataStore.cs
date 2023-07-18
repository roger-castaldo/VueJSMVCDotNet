using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    internal class DataStore : IDataStore
    {
        private Dictionary<string,object> _data;

        public DataStore()
        {
            _data=new Dictionary<string, object>();
        }
        public object this[string key] { get => (_data.ContainsKey(key) ? _data[key] : null);
            set
            {
                if (_data.ContainsKey(key))
                    _data.Remove(key);
                if (value!=null)
                    _data.Add(key, value);
            }
        }
    }
}
