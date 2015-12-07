using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace myMangaSiteExtension.Utilities
{
    public static class StringBuilderExtensions
    {
        public static void AppendUrlEncoded(this StringBuilder stringBuider, String name, String value, Boolean firstValue = false)
        {
            if (!firstValue)
            { stringBuider.Append("&"); }
            stringBuider.Append(Uri.EscapeDataString(name)).Append("=").Append(Uri.EscapeDataString(value));
        }
    }
}
