using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Core.Other
{
    public static class StringExtensions
    {
        /// <summary>
        /// Get the string slice between the two indexes.
        /// Inclusive for start index, exclusive for end index.
        /// </summary>
        public static String Slice(this String source, Int32 start, Int32 end)
        {
            if (start < 0) // Keep this for negative end support
                start = source.Length + start;
            if (end < 0) // Keep this for negative end support
                end = source.Length + end;
            Int32 len = end - start;               // Calculate length
            return source.Substring(start, len); // Return Substring of length
        }
    }

    public static class HtmlAgilityPackExtensions
    {
        public static Dictionary<String, String> Style(this HtmlNode Node)
        {
            Dictionary<String, String> Style = new Dictionary<String, String>();
            if (Node.Attributes.Contains("style"))
            {
                String[] styles = Node.Attributes["style"].Value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (String style in styles)
                    if (style.Contains(':'))
                        Style.Add(style.Slice(0, style.IndexOf(':')).ToLower(), style.Substring(style.IndexOf(':') + 1));
            }
            return Style;
        }
    }

    public static class Arrays
    {
        public static void Concat<T>(this T[] array, params T[][] arrays)
        { Arrays.Concat(array, arrays); }
        public static void Concat<T>(this T[] array, IEnumerable<T[]> arrays)
        { foreach (T[] a in arrays) array.Concat(a); }

        public static T[] Concat<T>(params T[][] arrays) { return Concat(arrays.AsEnumerable()); }
        public static T[] Concat<T>(IEnumerable<T[]> arrays)
        {
            T[] array = new T[0];
            foreach (T[] a in arrays)
                array.Concat(a);
            return array;
        }
    }

    public static class Lists
    {
        public static void Concat<T>(this List<T> list, params List<T>[] lists)
        { Lists.Concat(list, lists.AsEnumerable()); }
        public static void Concat<T>(this List<T> list, IEnumerable<List<T>> lists)
        { foreach (List<T> l in lists) list.AddRange(l); }

        public static List<T> Concat<T>(params List<T>[] lists)
        { return Concat(lists.AsEnumerable()); }
        public static List<T> Concat<T>(IEnumerable<List<T>> lists)
        {
            List<T> list = new List<T>();
            foreach (List<T> l in lists)
                list.AddRange(l);
            return list;
        }
    }

    public static class IComparableExtensions
    {
        public static Boolean InRange<T>(this T val, T from, T to) where T : IComparable<T>
        { return val.CompareTo(from) >= 1 && val.CompareTo(to) <= -1; }
    }
}
