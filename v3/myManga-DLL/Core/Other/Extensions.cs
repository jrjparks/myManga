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
                        Style.Add(style.Slice(0, style.IndexOf(':')), style.Substring(style.IndexOf(':') + 1));
            }
            return Style;
        }
    }
}
