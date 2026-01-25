using System.Collections.Generic;

namespace AutomatedTesting
{
    internal class DataStore : IDataStore
    {
        private Dictionary<string, object> data = new();

        public object? this[string key]
        {
            get
            {
                if (data.TryGetValue(key, out var value))
                    return value;
                return null;
            }
            set
            {
                data.Remove(key);
                if (value!=null)
                    data.Add(key, value);
            }
        }
    }
}
