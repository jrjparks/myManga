using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BakaBox.Extensions
{
    public static class IEnumeratorHelperMembers
    {
        public static Int32 Count<T>(this IEnumerable<T> Collection)
        { return Collection.Count(); }
    }
}
