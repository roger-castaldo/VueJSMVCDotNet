using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VueJSMVCDotNet
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enu, Action<T> action)
        {
            foreach (T item in enu) action(item);
            return enu; // make action Chainable/Fluent
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enu, Action<T,int> action)
        {
            int idx = 0;
            foreach (T item in enu)
            {
                action(item, idx);
                idx++;
            }
            return enu; // make action Chainable/Fluent
        }
    }
}
