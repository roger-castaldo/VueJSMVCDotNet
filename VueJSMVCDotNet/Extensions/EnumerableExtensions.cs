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

        public static int IndexOf<T>(this IEnumerable<T> enu, Func<T, bool> match)
            => enu.Select((i, index) => new { i, index })
            .FirstOrDefault(p => match(p.i))?.index??-1;
    }
}
