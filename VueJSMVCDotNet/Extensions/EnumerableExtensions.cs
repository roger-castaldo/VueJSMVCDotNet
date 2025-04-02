namespace VueJSMVCDotNet.Extensions
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enu, Action<T> action)
        {
            foreach (T item in enu) action(item);
            return enu; // make action Chainable/Fluent
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enu, Action<T, int> action)
        {
            int idx = 0;
            foreach (T item in enu)
            {
                action(item, idx);
                idx++;
            }
            return enu; // make action Chainable/Fluent
        }

        public static R? SelectFirstOrDefault<T, R>(this IEnumerable<T> enu, Func<T, R> convert, Func<R, bool> match)
        {
            foreach (T item in enu)
            {
                var res = convert(item);
                if (match(res)) return res;
            }
            return default;
        }

        public static int IndexOf<T>(this IEnumerable<T> enu, Func<T, bool> match)
            => enu.Select((i, index) => new { i, index })
            .FirstOrDefault(p => match(p.i))?.index??-1;
    }
}
