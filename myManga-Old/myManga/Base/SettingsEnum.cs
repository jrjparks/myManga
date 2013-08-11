using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace myManga.Base
{
    public enum ChapterOrder
    {
        Ascending = 0x01,
        Descending = 0x02,
        Auto = Ascending | Descending
    }
}
