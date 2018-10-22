using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gibbed.Helpers
{
    public static class JoinHelper
    {
        public static string Implode<T>(
            this IEnumerable<T> source,
            Func<T, string> projection,
            string separator)
        {
            if (source.Count() == 0)
            {
                return "";
            }

            var builder = new StringBuilder();
            
            builder.Append(projection(source.First()));
            foreach (T element in source.Skip(1))
            {
                builder.Append(separator);
                builder.Append(projection(element));
            }

            return builder.ToString();
        }

        public static string Implode<T>(
            this IEnumerable<T> source,
            string separator)
        {
            return Implode(source, t => t.ToString(), separator);
        }
    }
}
