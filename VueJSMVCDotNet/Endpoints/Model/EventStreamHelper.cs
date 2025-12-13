using Microsoft.Extensions.Primitives;
using System.Net;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal static class EventStreamHelper
    {
        public const string NullKeyword = "null";
        public const string MessageEvent = "message";
        public const string CloseEvent = "close";

        private static readonly Dictionary<Type, Func<StringValues, object?>> QueryValueConverters = new()
        {
            [typeof(char?)] = (value) => CheckNullKeyword(value,(s)=>s[0]),
            [typeof(char)] = (value) => value.ToString()[0],
            [typeof(byte?)] = (value) => CheckNullKeyword(value,(s)=>byte.Parse(s)),
            [typeof(byte)] = (value) => byte.Parse(value.ToString()),
            [typeof(bool?)] = (value) => CheckNullKeyword(value, (s) => bool.Parse(s)),
            [typeof(bool)] = (value) => bool.Parse(value.ToString()),
            [typeof(short?)] = (value) => CheckNullKeyword(value, (s) => short.Parse(s)),
            [typeof(short)] = (value) => short.Parse(value.ToString()),
            [typeof(ushort?)] = (value) => CheckNullKeyword(value, (s) => ushort.Parse(s)),
            [typeof(ushort)] = (value) => ushort.Parse(value.ToString()),
            [typeof(int?)] = (value) => CheckNullKeyword(value, (s) => int.Parse(s)),
            [typeof(int)] = (value) => int.Parse(value.ToString()),
            [typeof(uint?)] = (value) => CheckNullKeyword(value, (s) => uint.Parse(s)),
            [typeof(uint)] = (value) => uint.Parse(value.ToString()),
            [typeof(long?)] = (value) => CheckNullKeyword(value, (s) => long.Parse(s)),
            [typeof(long)] = (value) => long.Parse(value.ToString()),
            [typeof(ulong?)] = (value) => CheckNullKeyword(value, (s) => ulong.Parse(s)),
            [typeof(ulong)] = (value) => ulong.Parse(value.ToString()),
            [typeof(double?)] = (value) => CheckNullKeyword(value, (s) => double.Parse(s)),
            [typeof(double)] = (value) => double.Parse(value.ToString()),
            [typeof(float?)] = (value) => CheckNullKeyword(value, (s) => float.Parse(s)),
            [typeof(float)] = (value) => float.Parse(value.ToString()),
            [typeof(decimal?)] = (value) => CheckNullKeyword(value, (s) => decimal.Parse(s)),
            [typeof(decimal)] = (value) => decimal.Parse(value.ToString()),
#pragma warning disable S6580 // Use a format provider when parsing date and time
            [typeof(DateTime?)] = (value) => CheckNullKeyword(value, (s) => DateTime.Parse(s)),
            [typeof(DateTime)] = (value) => DateTime.Parse(value.ToString()),
#pragma warning restore S6580 // Use a format provider when parsing date and time
            [typeof(Guid?)] = (value) => CheckNullKeyword(value, (s) => Guid.Parse(s)),
            [typeof(Guid)] = (value) => Guid.Parse(value.ToString()),
            [typeof(string)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : value.ToString()),
            [typeof(Version)] = (value) => CheckNullKeyword(value, (s) => Version.Parse(s)),
            [typeof(IPAddress)] = (value) => CheckNullKeyword(value, (s) => IPAddress.Parse(s)),
        };

        private static object? CheckNullKeyword(StringValues value, Func<string, object?> convert)
        {
            if (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase))
                return null;
            return convert(value.ToString());
        }

        public static bool IsUsableType(Type type)
        {
            if (QueryValueConverters.ContainsKey(type)||type.IsEnum)
                return true;
            var details = Utility.ExtractUnderlyingType(type);
            return details.type.IsEnum && !details.isTask && !details.isArray && !details.isValueTask;
        }

        public static object? ConvertValue(Type type, StringValues value)
        {
            if (QueryValueConverters.ContainsKey(type))
                return QueryValueConverters[type](value);
            var details = Utility.ExtractUnderlyingType(type);
            return (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : Enum.Parse(details.type, value.ToString()));
        }
    }
}
