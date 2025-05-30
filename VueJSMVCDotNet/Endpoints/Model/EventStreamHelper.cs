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
            [typeof(char?)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : value.ToString()[0]),
            [typeof(char)] = (value) => value.ToString()[0],
            [typeof(byte?)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : byte.Parse(value.ToString())),
            [typeof(byte)] = (value) => byte.Parse(value.ToString()),
            [typeof(bool?)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : bool.Parse(value.ToString())),
            [typeof(bool)] = (value) => bool.Parse(value.ToString()),
            [typeof(short?)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : short.Parse(value.ToString())),
            [typeof(short)] = (value) => short.Parse(value.ToString()),
            [typeof(ushort?)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : ushort.Parse(value.ToString())),
            [typeof(ushort)] = (value) => ushort.Parse(value.ToString()),
            [typeof(int?)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : int.Parse(value.ToString())),
            [typeof(int)] = (value) => int.Parse(value.ToString()),
            [typeof(uint?)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : uint.Parse(value.ToString())),
            [typeof(uint)] = (value) => uint.Parse(value.ToString()),
            [typeof(long?)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : long.Parse(value.ToString())),
            [typeof(long)] = (value) => long.Parse(value.ToString()),
            [typeof(ulong?)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : ulong.Parse(value.ToString())),
            [typeof(ulong)] = (value) => ulong.Parse(value.ToString()),
            [typeof(double?)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : double.Parse(value.ToString())),
            [typeof(double)] = (value) => double.Parse(value.ToString()),
            [typeof(float?)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : float.Parse(value.ToString())),
            [typeof(float)] = (value) => float.Parse(value.ToString()),
            [typeof(decimal?)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : decimal.Parse(value.ToString())),
            [typeof(decimal)] = (value) => decimal.Parse(value.ToString()),
#pragma warning disable S6580 // Use a format provider when parsing date and time
            [typeof(DateTime?)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : DateTime.Parse(value.ToString())),
            [typeof(DateTime)] = (value) => DateTime.Parse(value.ToString()),
#pragma warning restore S6580 // Use a format provider when parsing date and time
            [typeof(Guid?)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : Guid.Parse(value.ToString())),
            [typeof(Guid)] = (value) => Guid.Parse(value.ToString()),
            [typeof(string)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : value.ToString()),
            [typeof(Version)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : Version.Parse(value.ToString())),
            [typeof(IPAddress)] = (value) => (value.ToString().Equals(NullKeyword, StringComparison.InvariantCultureIgnoreCase) ? null : IPAddress.Parse(value.ToString())),
        };

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
