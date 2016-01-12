using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace myMangaSiteExtension.Utilities
{
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
}
