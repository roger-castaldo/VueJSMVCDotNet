using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model
{
    internal class ModelRequestData : IRequestData
    {
        private readonly Dictionary<string, object> _formData;
        public IEnumerable<string> Keys => _formData.Keys;

        public T GetValue<T>(string key)
        {
            key = Keys.FirstOrDefault(k => String.Equals(k, key, StringComparison.InvariantCultureIgnoreCase));
            if (key==null)
                throw new KeyNotFoundException();
            var obj = _formData[key];
            try
            {
                if (obj is JsonDocument document)
                    return Utility.JsonDecode<T>(document,_session);
                else if (obj is JsonNode node)
                    return Utility.JsonDecode<T>(node, _session);
                else if (obj is JsonElement element)
                    return Utility.JsonDecode<T>(element, _session);
                else
                    return Utility.ConvertToType<T>(obj, _session);
            }
            catch (Exception)
            {
                throw new InvalidCastException();
            }
        }

        internal object GetValue(Type t,string key) {
            try
            {
                return GetType().GetMethod("GetValue").MakeGenericMethod(new Type[] { t }).Invoke(this, new object[] { key });
            }catch (Exception)
            {
                throw new InvalidCastException();
            }
        }

        private readonly ISecureSession _session;
        public ISecureSession Session => _session;

        public ModelRequestData(Dictionary<string, object> formData, ISecureSession session)
        {
            _formData = formData;
            _session = session;
        }
    }
}
